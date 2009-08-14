//
// $Id$
//
//
// Original author: Darren Kessner <darren@proteowizard.org>
//
// Copyright 2007 Spielberg Family Center for Applied Proteomics
//   Cedars Sinai Medical Center, Los Angeles, California  90048
//
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
//


#include "IntegerSet.hpp"
#include "pwiz/utility/misc/unit.hpp"
#include <iostream>
#include <vector>
#include <iterator>
#include <cstring>


using namespace std;
using namespace pwiz::util;


ostream* os_ = 0;


void test()
{
    // instantiate IntegerSet

    IntegerSet a;
    unit_assert(a.empty());

    a.insert(1);
    unit_assert(!a.empty());

    a.insert(2);
    a.insert(IntegerSet::Interval(0,2));
    a.insert(0,2);
    a.insert(4);

    // verify virtual container contents: 0, 1, 2, 4

    if (os_)
    {            
        copy(a.begin(), a.end(), ostream_iterator<int>(*os_," ")); 
        *os_ << endl;
    }

    vector<int> b; 
    copy(a.begin(), a.end(), back_inserter(b));

    unit_assert(b.size() == 4);
    unit_assert(b[0] == 0);
    unit_assert(b[1] == 1);
    unit_assert(b[2] == 2);
    unit_assert(b[3] == 4);

    // insert [2,4], and verify contents: 0, 1, 2, 3, 4

    a.insert(2,4);

    if (os_)
    {            
        copy(a.begin(), a.end(), ostream_iterator<int>(*os_," ")); 
        *os_ << endl;
    }

    b.clear();
    copy(a.begin(), a.end(), back_inserter(b));

    unit_assert(b.size() == 5);
    for (int i=0; i<5; i++)
        unit_assert(i == b[i]);
}


void testInstantiation()
{
    IntegerSet a(666);
    vector<int> b;
    copy(a.begin(), a.end(), back_inserter(b));
    unit_assert(b.size() == 1);
    unit_assert(b[0] == 666);

    IntegerSet c(666,668);
    vector<int> d;
    copy(c.begin(), c.end(), back_inserter(d));
    unit_assert(d.size() == 3);
    unit_assert(d[0] == 666);
    unit_assert(d[1] == 667);
    unit_assert(d[2] == 668);
}


void testContains()
{
    IntegerSet a(3,5);
    a.insert(11);
    a.insert(13,17);

    for (int i=0; i<3; i++)
        unit_assert(!a.contains(i));
    for (int i=3; i<6; i++)
        unit_assert(a.contains(i));
    for (int i=6; i<11; i++)
        unit_assert(!a.contains(i));
    unit_assert(a.contains(11));
    unit_assert(!a.contains(12));
    for (int i=13; i<18; i++)
        unit_assert(a.contains(i));
    for (int i=18; i<100; i++)
        unit_assert(!a.contains(i));
}


void testUpperBound()
{
    IntegerSet a(3,5);

    for (int i=0; i<5; i++)
        unit_assert(!a.hasUpperBound(i));
    for (int i=5; i<10; i++)
        unit_assert(a.hasUpperBound(i));
}


void testIntervalExtraction()
{
    IntegerSet::Interval i;

    istringstream iss(" \t [-2 , 5] ");
    iss >> i;

    unit_assert(i.begin == -2);
    unit_assert(i.end == 5);
}



void testIntExtraction()
{
    //std::locale::global(std::locale("C"));  // hack for msvc

    istringstream iss("1,100");
    iss.imbue(locale("C")); // hack for msvc

    int i = 0;
    iss >> i;

    unit_assert(i == 1); 
}


void testParse()
{
    IntegerSet a;

    a.parse(" [-3, 2] [5 ,5] [ 8 , 9 ] booger ");  // insert(-3,2); insert(5); insert(8,9);

    vector<int> b;
    copy(a.begin(), a.end(), back_inserter(b));
    unit_assert(b.size() == 9);
    unit_assert(b[0] == -3);
    unit_assert(b[1] == -2);
    unit_assert(b[2] == -1);
    unit_assert(b[3] == 0);
    unit_assert(b[4] == 1);
    unit_assert(b[5] == 2);
    unit_assert(b[6] == 5);
    unit_assert(b[7] == 8);
    unit_assert(b[8] == 9);
}


int main(int argc, char* argv[])
{
    try
    {
        if (argc>1 && !strcmp(argv[1],"-v")) os_ = &cout;
        test();
        testInstantiation();
        testContains();
        testUpperBound();
        testIntervalExtraction();
        testIntExtraction();
        testParse();
        return 0;
    }
    catch (exception& e)
    {
        cerr << e.what() << endl;
        return 1;
    }
}


