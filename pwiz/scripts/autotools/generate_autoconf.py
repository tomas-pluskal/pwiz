#!/usr/bin/python
#  this is called by the script "makemake.sh"
#
# args are:
#   full_path_to_pwiz_root (where scripts, pwiz_tools, pwiz etc are found)
#   full_path_to_tmpdir (where build.log is found)

# notes on autotools and pwiz
#
#
#
# This is about producing a GNU standard library installer for pwiz
# that means the classic "./configure ; make ; make install" sequence
# No bjam involved.  We do this by observing a bjam -d+2 build in operation
# and driving autoconf from that.
#
# In particular we wish to provide standard targets:
# "all" (build pwiz lib and examples), "install", "check" (runs unit tests)
#
# We're assuming the user has a prebuilt boost lib already installed.
# note on Ubuntu (10.04) boost official packages are 1.40 so this required
# installing from source = i used 1.48
#
# 

import autotools_common as ac

# minimum version of boost known to work with pwiz
min_boost_ver="1.43.0"

# minimum version of autotools known to work with our stuff
min_autotools_ver="2.60"

# any compiler -D stuff we might need (we add the actual -D below)
compiler_defines = []
compiler_defines.append("WITHOUT_MZ5") # let's not worry about the mz5 thing at this point

import os
import sys
import stat
import tarfile

if (len(sys.argv) < 3) :
	print "usage: %s <pwizroot> <dir_with_build_log>"%sys.argv[0]
	print "# normally this is called by the script makemake.sh"
	quit(1)
	

ac.set_pwizroot(sys.argv[1])
workdir = sys.argv[2]
logfile = "%s/build.log"%workdir
print "using log file %s\n"%logfile

def make_testname(testpath) : # /foo/bar/baz.cpp
	noext = os.path.basename(testpath).rpartition(".")[0] # baz
	dirname = testpath.rpartition("/")[0].rpartition("/")[2] # bar
	return dirname+"_"+noext # bar_baz

def relname(fname) : # /mnt/hgfs/ProteoWizard/pwiz/foo/bar/baz
	return fname.replace(ac.get_pwizroot(),".") # ./foo/bar/baz

def isTestFile(file) :
	return "Test." in file or "test." in file

tests=set()
srcs=set()
testhelpers=set()
testargs=dict()
libnames=set()
examples=set()
includes=set()
shipdirs=set() # set of directories of interest


def isTestFile(file) :
	return "Test." in file or "test." in file

def addShipDir(in_d,addTree=False) :
	if not ".svn" in in_d :
		d = os.path.abspath(in_d)
		shipdirs.add(d)
		if addTree:
			for dd in os.listdir(d) :
				ddd = d+"/"+dd
				if stat.S_ISDIR(os.stat(ddd).st_mode) :
					addShipDir(ddd,addTree)

def addFile(file) :
	if not ".svn" in file :
		addShipDir(os.path.dirname(file))
		if ("/pwiz/libraries/libsvm" in file):
			if ("libsvm" in file):
				srcs.add(file)
				includes.add(relname(file.rpartition("/")[0]))
		elif ac.isExampleFile(file) : 
			if ("/pwiz_tools/examples/" in file) : # need a full path
				examples.add(file)
			else :
				examples.add(file.replace("pwiz_tools/examples/","%s/pwiz_tools/examples/"%ac.get_pwizroot()))
		elif isTestFile(file) :
			tests.add(file)
		else :
			srcs.add(file)




# assume a build log generated by "bjam -d+2"
for line in open(logfile):
	if ("g++" in line or "gcc" in line) and "-c" in line and '.o" "' in line:
		line = line.replace("\\","/") # in case its mingw gcc
		file = (line.rpartition(".o\" \"")[2]).rpartition("\"")[0]
		if ac.isSrcFile(file) or ac.isExampleFile(file):
			addFile(file)
		if ".archive" in line :
			if "\\" in line: 
				libname = line.rpartition("\\")[2]
			else :
				libname = line.rpartition("/")[2]
			libnames.add(libname.rpartition(".")[0])
		iseek = line
		# 
		# locate any explicit includes
		pwizinc = ' -I"%s/pwiz'%ac.get_pwizroot()
		while (pwizinc in iseek) :
			incl = iseek.rpartition(pwizinc)[2]
			includes.add("pwiz"+incl.partition('"')[0])
			iseek = iseek.rpartition(pwizinc)[0]
		if ("libraries/boost_aux" in line) : # forward looking boostiness
			includes.add("libraries/boost_aux")
#print tests
#print srcs
# print libnames
#print includes

# nab the version info from the generated version.cpp file
version=[]
for line in open ("%s/pwiz/Version.cpp"%ac.get_pwizroot()) :
	if "{return" in line:
		version.append((line.rpartition("{return ")[2]).rpartition(";")[0])

pkgname = "libpwiz"
versionDotted = "%s.%s.%s"%(version[0],version[1],version[2])

# boost libs stuff -  putting "threads" first matters
boostlibs=["THREADS","FILESYSTEM","REGEX","SERIALIZATION","SYSTEM","IOSTREAMS","DATE_TIME","PROGRAM_OPTIONS"]

f = open('%s/configure.ac'%workdir, 'w')
for configac in open("%s/pwiz/configure.scan"%ac.get_pwizroot()) :
	configac=configac.replace("FULL-PACKAGE-NAME",pkgname)
	configac=configac.replace("VERSION",versionDotted)
	if ("AC_INIT" in configac) :
		configac = configac + "AM_INIT_AUTOMAKE\n"
	if ("# Checks for programs" in configac) :
		configac = configac +"AC_PROG_LIBTOOL\n"
	if ("AC_PREREQ([" in configac) :
		configac = "AC_PREREQ(["+min_autotools_ver+"])\n"
	configac=configac.replace("AC_CONFIG_SRCDIR([","AC_CONFIG_SRCDIR([../pwiz/")
	configac=configac.replace("BUG-REPORT-ADDRESS","proteowizard-support@lists.sourceforge.net")
	# we're building libpwiz, so don't demand pwiz_lib_*
	configac=configac.replace("AC_CHECK_LIB([pwiz_","#AC_CHECK_LIB([pwiz_")
	# we don't use config.h
	configac=configac.replace("AC_CONFIG_HEADERS","#AC_CONFIG_HEADERS")
	if ("AC_OUTPUT" in configac) : # last line, add anything else now!
		f.write("BOOST_REQUIRE([%s])\n"%min_boost_ver)
		for lib in boostlibs :
			f.write("BOOST_%s\n"%lib)
		f.write("AC_CONFIG_FILES([Makefile])\n")
	f.write(configac)
f.close()

makefileam = open('%s/Makefile.am'%workdir, 'w')
# subdir-objects avoids trouble with multiple Version.cpp files etc
makefileam.write('AUTOMAKE_OPTIONS = subdir-objects\n')
libname = '%s.la'%(pkgname)
makefileam.write('lib_LTLIBRARIES = %s\n'%libname)
makefileam.write('LDADD = %s $(LIBS)\n'%libname) # for the examples to link with

# note using https://github.com/tsuna/boost.m4 for autoconf boost detection
makefileam.write('ACLOCAL_AMFLAGS = -I .\n') # per https://raw.github.com/tsuna/boost.m4/master/README
makefileam.write('LIBS = $(BOOST_THREAD_LIBS)') # there's some disconnect between BOOST_THREADS* and BOOST_THREAD*
for lib in boostlibs :
	makefileam.write(" $(BOOST_%s_LIBS)"%lib)
makefileam.write('\n')
makefileam.write('AM_LDFLAGS =')
for lib in boostlibs :
	makefileam.write(' $(BOOST_%s_LDFLAGS)'%lib)
makefileam.write('\n')
amcppflags = 'AM_CPPFLAGS = $(BOOST_CPPFLAGS)'
for inc in includes :
	amcppflags += ' -I\"%s\"'%inc
makefileam.write(amcppflags)
for defined in compiler_defines:
	makefileam.write(" -D%s"%defined)
makefileam.write("\n")

# write the source dependencies
la_name = libname.replace(".","_")
makefileam.write('%s_SOURCES = '%la_name)
for src in srcs :
	makefileam.write(' %s'%relname(src))
makefileam.write('\n')
makefileam.write('%s_LDFLAGS = -version-info %s:%s:0\n'%(la_name,version[0],version[1]))

# write the test programs
makefileam.write("#\n# here are some programs that test the pwiz library.\n#\n")
makefileam.write('check_PROGRAMS =')
for test in tests :
	makefileam.write(' %s'%make_testname(test))
makefileam.write('\n')
for test in tests :
	tname = make_testname(test)
	makefileam.write("%s_SOURCES=%s\n"%(tname,relname(test)))

# write the example programs
makefileam.write("#\n# here are some examples of programs that use the pwiz library.\n#\n")
makefileam.write('bin_PROGRAMS =')
for example in examples :
	makefileam.write(" %s"%os.path.basename(example).rpartition(".")[0])
makefileam.write('\n')
for example in examples :
	ename = os.path.basename(example).rpartition(".")[0]
	makefileam.write("%s_SOURCES=%s\n"%(ename,relname(example)))
	
makefileam.close()

# create a source tarball 
for ipath in includes :
	addShipDir(ipath)
addShipDir(workdir)
addShipDir(ac.get_pwizroot())

# include the whole boost_aux tree
for shipdir in shipdirs :
	if "boost_aux" in shipdir :
		addShipDir(shipdir,addTree=True)
		break
for d in ["pwiz_aux"] : # any others not mentioned?
	addShipDir(ac.get_pwizroot()+"/"+d,addTree=True)

fz="libpwiz_src.tgz"
print "creating autotools source build distribution kit %s"%(fz)
z = tarfile.open(fz,"w|gz")
exts = ["h","hpp","c","cpp","cxx","am","inl"]

for shipdir in shipdirs :
	for file in os.listdir(shipdir) :
		f = shipdir+"\\"+file
		ext = file.partition(".")[2]
		if (not stat.S_ISDIR(os.stat(f).st_mode)) and ext in exts or ext=="":
			z.add(f,ac.replace_pwizroot(f,"pwiz"))
testfiles = set()
for test in testargs : # grab data files
	f = absname(testargs[test])
	if (os.path.exists(f)) :
		ext = f.rpartition(".")[2]
		d = os.path.dirname(f) # go ahead and grab anything else with same .ext
		for file in os.listdir(d) :
			ff = d+"\\"+file
			ext2 = ff.rpartition(".")[2]
			if (ext==ext2 and not stat.S_ISDIR(os.stat(ff).st_mode)):
				testfiles.add(ff)
for f in testfiles :
	z.add(f,ac.replace_pwizroot(f,"pwiz"))
z.close()

