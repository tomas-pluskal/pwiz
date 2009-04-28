///
/// WarpFunction.cpp
///


#include "WarpFunction.hpp"
#include "pwiz/utility/math/Stats.hpp"
#include "pwiz/utility/math/Stats.cpp" //TODO: figure out why this is required to avoid the linker error

#include <iostream>
#include <fstream>

using namespace std;
using namespace pwiz::eharmony;

WarpFunction::WarpFunction(const vector<pair<double,double> >& anchors) : anchors_(anchors){}
void WarpFunction::operator()(vector<double>& rt_vals, vector<double>& warped_rt_vals)
{
  //copy rt_vals to warped_rt_vals
  warped_rt_vals.assign(rt_vals.begin(),rt_vals.end());
}

LinearWarpFunction::LinearWarpFunction(vector<pair<double,double> >& anchors) : WarpFunction(anchors)
{
  vector<pair<double,double> >::iterator anchor_it = anchors_.begin();
  double rt1MeanSum = 0;
  double rt2MeanSum = 0;

  for(; anchor_it != anchors_.end(); ++anchor_it)
    {
        rt1MeanSum += anchor_it->first;
        rt2MeanSum += anchor_it->second;

    }

  double rt1Mean = rt1MeanSum / anchors_.size();
  double rt2Mean = rt2MeanSum / anchors_.size();
 
  double numeratorSum = 0;
  double denominatorSum = 0;
  
  vector<pair<double,double> >::iterator ss_it = anchors_.begin();
  for(; ss_it != anchors_.end(); ++ss_it)
    {
        numeratorSum += ss_it->first*ss_it->second;
        denominatorSum += ss_it->first*ss_it->first;
     
    }

  double m = (numeratorSum - anchors_.size()*rt1Mean*rt2Mean)/(denominatorSum - anchors_.size()*rt1Mean*rt1Mean);
  double b = rt2Mean - m*rt1Mean;

  coefficients_.push_back(b);
  coefficients_.push_back(m);
 

}

void LinearWarpFunction::operator()(vector<double>& rt_vals, vector<double>& warped_rt_vals)
{
  vector<double>::iterator rt_it = rt_vals.begin();
  for(; rt_it != rt_vals.end(); ++rt_it)
    {
      double warped_val = coefficients_.at(0) + coefficients_.at(1)*(*rt_it);
      warped_rt_vals.push_back(warped_val);

    }

  return;
}

PiecewiseLinearWarpFunction::PiecewiseLinearWarpFunction(vector<pair<double,double> >& anchors) : WarpFunction(anchors)
{
  sort(anchors_.begin(), anchors_.end());
  vector<pair<double,double> >::iterator anchor_it = anchors_.begin();
  vector<pair<double,double> >::iterator anchor_it_plus = anchors_.begin() + 1;

  double m_0;
  if (anchors_.begin()->first == 0) m_0 = 0; // shouldn't worry about negative numbers.. if zero anchor for strange reason, first slope is default to zero so as to not break anything with a div by zero
  else m_0 = anchors_.begin()->second / anchors_.begin()->first;

  double b_0 = anchors_.begin()->second - m_0 * anchors_.begin()->first;

  pair<double,double> first_piece(b_0,m_0);
  coefficients_.push_back(first_piece);

  for(; anchor_it_plus != anchors_.end(); ++anchor_it,++anchor_it_plus)
    {    
      double m = (anchor_it_plus->second - anchor_it->second)/(anchor_it_plus->first - anchor_it->first);
      double b = anchor_it_plus->second - m * anchor_it_plus->first;

      pair<double,double> piece_coefficients(b,m);
      coefficients_.push_back(piece_coefficients);
     
    }

}

bool first_less_than(pair<int,double> a, pair<int,double> b)
{
  return a.first < b.first;

}

bool second_less_than(pair<int,double> a, pair<int,double> b)
{
  return a.second < b.second;

}

void PiecewiseLinearWarpFunction::operator()(vector<double>& rt_vals, vector<double>& warped_rt_vals)
{
  //if rt_vals is not sorted, will need to sort, but then store original place, and output in the same order again
  // multimap? 

  vector<pair<int,double> > rt_table;
  vector<pair<int,double> > warped_rt_table;
  
  vector<double>::iterator rt_it = rt_vals.begin();
  size_t index = 0;

  for(; rt_it != rt_vals.end(); ++rt_it, ++index)
    {
      pair<int,double> table_entry(index,*rt_it);
      rt_table.push_back(table_entry);

    }
  
  sort(rt_table.begin(),rt_table.end(), second_less_than);
  sort(anchors_.begin(), anchors_.end());

  vector<pair<int,double> >::iterator rt_table_it = rt_table.begin();
  vector<pair<double,double> >::iterator anchor_it = anchors_.begin();
  vector<pair<double,double> >::iterator coeff_it = coefficients_.begin();

  if (anchors_.size() != coefficients_.size()) throw runtime_error("[WarpFunction] wrong size");
  for(; rt_table_it != rt_table.end() ; ++rt_table_it)  
    {      
      bool done = false;
      while (!done)
          {
              if(rt_table_it->second < anchor_it->first || anchor_it + 1 == anchors_.end())
                  {
                      double warped_rt = coeff_it->first + coeff_it->second * rt_table_it->second; //y = b + mx
                      pair<int,double> warped_table_entry(rt_table_it->first, warped_rt);
                      warped_rt_table.push_back(warped_table_entry);
                      done = true;

                  }

              else 
                  {
                      ++anchor_it;
                      ++coeff_it;

                  }

          }

    }


  sort(warped_rt_table.begin(),warped_rt_table.end(),first_less_than);
 
  vector<pair<int,double> >::iterator warped_rt_table_it = warped_rt_table.begin();
  for(;warped_rt_table_it != warped_rt_table.end(); ++warped_rt_table_it)

    {
        warped_rt_vals.push_back(warped_rt_table_it->second);

    }

}

