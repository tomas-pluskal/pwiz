# Copyright (c) 2005 Alexey Pakhunov.
# Copyright (c) 2011 Juraj Ivancic
#
# Use, modification and distribution is subject to the Boost Software
# License Version 1.0. (See accompanying file LICENSE_1_0.txt or
# http://www.boost.org/LICENSE_1_0.txt)

# Microsoft Interface Definition Language (MIDL) related routines
from b2.build import scanner, type
from b2.build.toolset import flags
from b2.build.feature import feature
from b2.manager import get_manager
from b2.tools import builtin, common
from b2.util import regex

def init():
    pass

type.register('IDL', ['idl'])

# A type library (.tlb) is generated by MIDL compiler and can be included
# to resources of an application (.rc). In order to be found by a resource 
# compiler its target type should be derived from 'H' - otherwise
# the property '<implicit-dependency>' will be ignored.
type.register('MSTYPELIB', 'tlb', 'H')

# Register scanner for MIDL files
class MidlScanner(scanner.Scanner):
    def __init__ (self, includes=[]):
        scanner.Scanner.__init__(self)
        self.includes = includes

        # List of quoted strings 
        re_strings = "[ \t]*\"([^\"]*)\"([ \t]*,[ \t]*\"([^\"]*)\")*[ \t]*" ;

        # 'import' and 'importlib' directives 
        self.re_import    = "import" + re_strings + "[ \t]*;" ;
        self.re_importlib = "importlib[ \t]*[(]" + re_strings + "[)][ \t]*;" ;

        # C preprocessor 'include' directive
        self.re_include_angle  = "#[ \t]*include[ \t]*<(.*)>" ;
        self.re_include_quoted = "#[ \t]*include[ \t]*\"(.*)\"" ;

    def pattern():
        # Match '#include', 'import' and 'importlib' directives
        return "((#[ \t]*include|import(lib)?).+(<(.*)>|\"(.*)\").+)"

    def process(self, target, matches, binding):
        included_angle  = regex.transform(matches, self.re_include_angle)
        included_quoted = regex.transform(matches, self.re_include_quoted)
        imported        = regex.transform(matches, self.re_import, [1, 3])
        imported_tlbs   = regex.transform(matches, self.re_importlib, [1, 3])

        # CONSIDER: the new scoping rule seem to defeat "on target" variables.
        g = bjam.call('get-target-variable', target, 'HDRGRIST')
        b = os.path.normalize_path(os.path.dirname(binding))

        # Attach binding of including file to included targets.
        # When target is directly created from virtual target
        # this extra information is unnecessary. But in other
        # cases, it allows to distinguish between two headers of the 
        # same name included from different places.      
        g2 = g + "#" + b

        g = "<" + g + ">"
        g2 = "<" + g2 + ">"

        included_angle  = [ g + x for x in included_angle  ]
        included_quoted = [ g + x for x in included_quoted ]
        imported        = [ g + x for x in imported        ]
        imported_tlbs   = [ g + x for x in imported_tlbs   ]

        all = included_angle + included_quoted + imported

        bjam.call('INCLUDES', [target], all)
        bjam.call('DEPENDS', [target], imported_tlbs)
        bjam.call('NOCARE', all + imported_tlbs)
        engine.set_target_variable(included_angle , 'SEARCH', ungrist(self.includes))
        engine.set_target_variable(included_quoted, 'SEARCH', b + ungrist(self.includes))
        engine.set_target_variable(imported       , 'SEARCH', b + ungrist(self.includes))
        engine.set_target_variable(imported_tlbs  , 'SEARCH', b + ungrist(self.includes))
        
        get_manager().scanners().propagate(type.get_scanner('CPP', PropertySet(self.includes)), included_angle + included_quoted)
        get_manager().scanners().propagate(self, imported)

scanner.register(MidlScanner, 'include')
type.set_scanner('IDL', MidlScanner)


# Command line options
feature('midl-stubless-proxy', ['yes', 'no'], ['propagated'] )
feature('midl-robust', ['yes', 'no'], ['propagated'] )

flags('midl.compile.idl', 'MIDLFLAGS', ['<midl-stubless-proxy>yes'], ['/Oicf'     ])
flags('midl.compile.idl', 'MIDLFLAGS', ['<midl-stubless-proxy>no' ], ['/Oic'      ])
flags('midl.compile.idl', 'MIDLFLAGS', ['<midl-robust>yes'        ], ['/robust'   ])
flags('midl.compile.idl', 'MIDLFLAGS', ['<midl-robust>no'         ], ['/no_robust'])

# Architecture-specific options
architecture_x86 = ['<architecture>' , '<architecture>x86']
address_model_32 = ['<address-model>', '<address-model>32']
address_model_64 = ['<address-model>', '<address-model>64']

flags('midl.compile.idl', 'MIDLFLAGS', [ar + '/' + m for ar in architecture_x86 for m in address_model_32 ], ['/win32'])
flags('midl.compile.idl', 'MIDLFLAGS', [ar + '/<address-model>64' for ar in architecture_x86], ['/x64'])
flags('midl.compile.idl', 'MIDLFLAGS', ['<architecture>ia64/' + m for m in address_model_64], ['/ia64'])

flags('midl.compile.idl', 'DEFINES', [], ['<define>'])
flags('midl.compile.idl', 'UNDEFS', [], ['<undef>'])
flags('midl.compile.idl', 'INCLUDES', [], ['<include>'])


builtin.register_c_compiler('midl.compile.idl', ['IDL'], ['MSTYPELIB', 'H', 'C(%_i)', 'C(%_proxy)', 'C(%_dlldata)'], [])


# MIDL does not always generate '%_proxy.c' and '%_dlldata.c'. This behavior 
# depends on contents of the source IDL file. Calling TOUCH_FILE below ensures
# that both files will be created so bjam will not try to recreate them 
# constantly.
get_manager().engine().register_action(
    'midl.compile.idl',
    '''midl /nologo @"@($(<[1]:W).rsp:E=
"$(>:W)"
-D$(DEFINES)
"-I$(INCLUDES)"
-U$(UNDEFS)
$(MIDLFLAGS)
/tlb "$(<[1]:W)"
/h "$(<[2]:W)"
/iid "$(<[3]:W)"
/proxy "$(<[4]:W)"
/dlldata "$(<[5]:W)")"
{touch} "$(<[4]:W)"  
{touch} "$(<[5]:W)"'''.format(touch=common.file_creation_command()))
