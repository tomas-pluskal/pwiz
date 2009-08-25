///
/// DatabaseQuery.cpp
///

#include "DatabaseQuery.hpp"
#include "PeptideID_dataFetcher.hpp"

using namespace std;
using namespace pwiz;
using namespace pwiz::eharmony;

const double pi = 3.1415926;

pair<double,double> DatabaseQuery::calculateSearchRegion(const double& mu1, const double& mu2, const double& sigma1, const double& sigma2, const double& threshold)
{
    double k1 = 1 + 2/sqrt(pi) * ( -2*mu2/(sigma2*sqrt(2)) + 2*pow(mu2,3)/(3*pow(sigma2*sqrt(2), 3)));
    double k2 = 1 + erf(-mu1/(sigma1*sqrt(2)));

    double mzDiameter = sqrt((sqrt(pi)/2*(threshold/k1 + k2 - 1) + 2*mu1/(sigma1*sqrt(2))) * 3*pow(sigma1*sqrt(2),3)/2 - pow(mu1,3));

    k1 = 1 + 2/sqrt(pi) * ( -2*mu1/(sigma1*sqrt(2)) + 2*pow(mu1,3)/(3*pow(sigma1*sqrt(2), 3)));
    k2 = 1 + erf(-mu2/(sigma2*sqrt(2)));

    double rtDiameter = sqrt((sqrt(pi)/2*(threshold/k1 + k2 - 1) + 2*mu2/(sigma2*sqrt(2))) * 3*pow(sigma2*sqrt(2),3)/2 - pow(mu2,3));

    return make_pair(mzDiameter, rtDiameter);

}

pair<double,double> DatabaseQuery::calculateNormalSearchRegion(const double& mu1, const double& mu2, double& sigma1, double& sigma2, const double& threshold)
{

    // recalculate for convolution
    sigma1 = sqrt(sigma1 * sigma1 + .001 *.001);
    sigma2 = sqrt(sigma2 * sigma2 + 100 * 100);

    //    double mzDiameter = fabs((1 - 2*mu1/(sqrt(2*pi)*sigma1))/(threshold - 2/(sqrt(2*pi)*sigma1)));
    //    double rtDiameter = fabs((1 - 2*mu2/(sqrt(2*pi)*sigma2))/(threshold - 2/(sqrt(2*pi)*sigma2)));

    // Non weighted
    double mzDiameter = fabs((threshold - 1) * (sqrt(pi)/2)*sqrt(2)*sigma1);
    double rtDiameter = fabs((threshold - 1) * (sqrt(pi)/2)*sqrt(2)*sigma2);
    
    mzDiameter *= 2;
    rtDiameter *= 2;

    //    cout << mzDiameter << "\t" << rtDiameter << endl;
    return make_pair(mzDiameter, rtDiameter);

}

vector<MatchPtr> DatabaseQuery::query(FeatureSequencedPtr fs, NormalDistributionSearch nds, double threshold)
{
    Bin<SpectrumQuery> bin = _database->getBin();
    pair<double,double> tolerances = calculateNormalSearchRegion(nds._mu_mz, nds._mu_rt,  nds._sigma_mz, nds._sigma_rt, threshold);
    bin.rebin(tolerances.first, tolerances.second);
    
    vector<SpectrumQueryPtr> candidates;
    pair<double,double> fsCoords = make_pair(fs->feature->mz, fs->feature->retentionTime);

    bin.getAdjacentBinContents(fsCoords, candidates);

    vector<MatchPtr> resultingMatches;

    vector<SpectrumQueryPtr>::iterator it = candidates.begin();
    for( ; it != candidates.end(); ++it) 
        {
            MatchPtr match(new Match(**it, fs->feature));
            match->score = nds.score(**it, *(fs->feature));
            if (match->feature->charge == match->spectrumQuery.assumedCharge && match->score > threshold)
                {
                    resultingMatches.push_back(match);

                }

        }

    return resultingMatches;

}

