///
/// Match_dataFetcher.cpp
///

#include "Match_dataFetcher.hpp"

using namespace pwiz;
using namespace eharmony;

namespace {

  vector<pair<pair<double,double>, Match> > getCoordinates(const vector<Match>& matches)
  {
      vector<Match>::const_iterator it = matches.begin();
      vector<pair<pair<double,double>, Match> > result;
      for(; it != matches.end(); ++it)
	  {
	      pair<double,double> coordinates(it->feature.mzMonoisotopic, it->feature.retentionTime); // no mspostfix
	      result.push_back(make_pair(coordinates, *it));

	  }
      
      return result;
  }


} // anonymous namespace

Match_dataFetcher::Match_dataFetcher(std::istream& is)
{
    MatchData md;
    md.read(is);
    vector<pair<pair<double,double>, Match> > objects = getCoordinates(md.matches);
    Bin<Match> bin(objects,.001,60);
    _bin = bin;

}

Match_dataFetcher::Match_dataFetcher(const MatchData& md)
{
    vector<pair<pair<double,double>, Match> > objects = getCoordinates(md.matches);
    Bin<Match> bin(objects,.001,60);
    _bin = bin;

}

void Match_dataFetcher::update(const Match& m)
{
    _bin.update(m, make_pair(m.feature.mzMonoisotopic, m.feature.retentionTime));

}

void Match_dataFetcher::erase(const Match& m)
{
    _bin.erase(m, make_pair(m.feature.mzMonoisotopic, m.feature.retentionTime));

}

void Match_dataFetcher::merge(const Match_dataFetcher& that)
{
    Bin<Match> bin = that.getBin();
    vector<boost::shared_ptr<Match> > matches = bin.getAllContents();
    vector<boost::shared_ptr<Match> >::iterator it = matches.begin();
    for(; it != matches.end(); ++it) update(**it);
    
}

vector<Match> Match_dataFetcher::getAllContents() const
{
  vector<boost::shared_ptr<Match> > hack = _bin.getAllContents();
  vector<boost::shared_ptr<Match> >::iterator it = hack.begin();
  vector<Match> result;
  for(; it != hack.end(); ++it) result.push_back(**it);

  return result;

}

vector<Match> Match_dataFetcher::getMatches(double mz, double rt)
{
    pair<double,double> coords = make_pair(mz,rt);
    vector<Match> result;
    _bin.getBinContents(coords,result);

    return result;

}

bool Match_dataFetcher::operator==(const Match_dataFetcher& that)
{
    return getAllContents() == that.getAllContents();;

}

bool Match_dataFetcher::operator!=(const Match_dataFetcher& that)
{
    return !(*this == that);

}
