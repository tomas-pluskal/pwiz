#!/bin/bash

function echo_info()
{
    echo "##teamcity[message text='$*']"
    echo "##teamcity[progressMessage '$*']"
}

function echo_error()
{
    echo "##teamcity[message text='$*' status='ERROR']"
    exit 1
}

echo_info "Cleaning project..."
if ! /bin/bash clean.sh
then
	echo_error "Error cleaning project!"
fi

# the -p1 argument overrides bjam's default behavior of merging stderr into stdout

echo_info "Generating revision info..."
if ! /bin/bash quickbuild.sh $1 -p1 pwiz//svnrev.hpp
then
	echo "Error generating revision info!"
	echo_error "Error generating revision info! See full build log for more details."
fi

echo_info "Running quickbuild.sh..."
if ! /bin/bash quickbuild.sh $1 -p1 ci=teamcity -j4
then
	echo "Error running quickbuild!"
	echo_error "Error running quickbuild! See full build log for more details."
fi

# uncomment this to test that test failures and error output are handled properly
/bin/bash quickbuild.sh $1 -p1 ci=teamcity pwiz/utility/misc//FailTest

exit 0
