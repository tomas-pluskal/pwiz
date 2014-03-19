﻿/*
 * Original author: Brian Pratt <bspratt .at. proteinms.net>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2014 University of Washington - Seattle, WA
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.ProteomeDatabase.API;
using pwiz.ProteomeDatabase.DataModel;
using pwiz.ProteomeDatabase.Fasta;
using pwiz.SkylineTestUtil;

namespace CommonTest
{

    public class FastaHeaderReaderResult : IEquatable<FastaHeaderReaderResult>
    {
        /// <summary>
        /// holds what we can learn from a FASTA header line
        /// </summary>
        /// <param name="accession">accession info</param>
        /// <param name="preferredname">human readable name</param>
        /// <param name="name">human readable name as parsed from fasta file with basic name-space-description pattern </param>
        /// <param name="description">human readable description as parsed from fasta file with basic name-space-description pattern</param>
        /// <param name="species">species, when known</param>
        /// <param name="gene">gene, when known</param>
        /// <param name="websearchcode">hint for faking up web search response</param>
        public FastaHeaderReaderResult(string accession, string preferredname, string name,
            string description,
            string species, string gene, char websearchcode = WebEnabledFastaImporter.UNIPROTKB_TAG)
        {
            Protein = new ProteinMetadata(name, description, preferredname, accession, gene, species, websearchcode.ToString(CultureInfo.InvariantCulture));
        }

        public ProteinMetadata Protein { get; set; }

        private static string FindTerm(string str, string splitter)
        {
            if ((str!=null) && str.Contains(splitter.Replace(@"\",""))) // Not L10N
            {
                var splits = Regex.Split(str, splitter, RegexOptions.IgnoreCase|RegexOptions.CultureInvariant); // Not L10N
                var after = Regex.Split(splits[1],@"[ |\.]")[0]; // Not L10N
                return after;
            }
            return null;
        }

        public static string FindTerm(ProteinMetadata protein, string splitter)
        {
            return FindTerm(protein.Name, splitter) ?? FindTerm(protein.Description, splitter);
        }

        public string GetIntermediateSearchterm(string initialSearchTerm)
        {
            // get the intermediate step - what entrez would return that we would take to uniprot
            if (initialSearchTerm != null)
            {
                var term = char.IsDigit(initialSearchTerm, 1) ? (FindTerm(Protein, @"ref\|") ?? FindTerm(Protein, @"SW\:") ?? FindTerm(Protein, @"pir\|\|")) : null; // hopefully go from GI to ref
                if (String.IsNullOrEmpty(term)) // xp_nnnn
                {
                     term = Protein.Accession; // no obvious hints in our test sets, just use the expected accession
                }
                return term;
            }
            return null;
        }

        public bool Equals(FastaHeaderReaderResult other)
        {
            return Equals(Protein, other.Protein);
        }
    }

    /// <summary>
    /// tests our ability to import various wildtype FASTA header lines, including some that need web services for full extraction
    /// </summary>
    [TestClass]
    public class FastaImporterTest : AbstractUnitTest
    {
        private const string NEGTEST = @">this is meant to fail"; // for use in negative test - Not L10N
        private const string novalue = null;


        public class FastaHeaderParserTest
        {
            public FastaHeaderParserTest(string header, FastaHeaderReaderResult[] expectedResults)
            {
                Header = header;
                ExpectedResults = expectedResults;
                // one or more sets of parsed parts (more than one with SOH-seperated header)
            }

            public string Header { get; private set; }
            public FastaHeaderReaderResult[] ExpectedResults { get; private set; }
        }

        private static List<FastaHeaderParserTest> GetTests()
        {

            return new List<FastaHeaderParserTest>
            {
                new FastaHeaderParserTest(
                    @">IPI:IPI00197700.1 Tax_Id=10116 Gene_Symbol=Apoa2 Apolipoprotein A-II",
                    new[]
                    {
                        new FastaHeaderReaderResult(accession: "P04638",
                            name: "IPI:IPI00197700.1",
                            preferredname: "APOA2_RAT",
                            description: "Tax_Id=10116 Gene_Symbol=Apoa2 Apolipoprotein A-II",
                            species: "Rattus norvegicus (Rat)", gene: "Apoa2")
                    }),

                new FastaHeaderParserTest(
                    ">ref|xp_915497.1| PREDICTED: similar to Syntaxin binding protein 3 (UNC-18 homolog 3) (UNC-18C) (MUNC-18-3) [Mus musculus].  Id=ref|XP_915497.1|gi|82891194| hash=28566A6F69346EB3",
                    new[]
                    {
                        new FastaHeaderReaderResult(accession: "Q60770", name: "ref|xp_915497.1|",
                            preferredname: "STXB3_MOUSE",
                            description:
                                "PREDICTED: similar to Syntaxin binding protein 3 (UNC-18 homolog 3) (UNC-18C) (MUNC-18-3) [Mus musculus].  Id=ref|XP_915497.1|gi|82891194| hash=28566A6F69346EB3",
                            species: "Mus musculus (Mouse)", gene: "Stxbp3 Stxbp3a Unc18c")
                    }),
                new FastaHeaderParserTest(
                    @">IPI:IPI00197700.1|SWISS-PROT:P04638|ENSEMBL:ENSRNOP00000004662|REFSEQ:NP_037244 Tax_Id=10116 Gene_Symbol=Apoa2 Apolipoprotein A-II",
                    new[]
                    {
                        new FastaHeaderReaderResult(accession: "P04638",
                            name: "IPI:IPI00197700.1|SWISS-PROT:P04638|ENSEMBL:ENSRNOP00000004662|REFSEQ:NP_037244",
                            preferredname: "APOA2_RAT",
                            description: "Tax_Id=10116 Gene_Symbol=Apoa2 Apolipoprotein A-II",
                            species: "Rattus norvegicus (Rat)", gene: "Apoa2")
                    }),

                new FastaHeaderParserTest(
                    ">SYHC Histidyl-tRNA synthetase, cytoplasmic OS=Homo sapiens GN=HARS PE=1 SV=2",
                    new[]
                    {
                        new FastaHeaderReaderResult(accession: novalue, name: "SYHC", preferredname: novalue,
                            description: "Histidyl-tRNA synthetase, cytoplasmic OS=Homo sapiens GN=HARS PE=1 SV=2",
                            species: "Homo sapiens", gene: "HARS")
                    }),

                new FastaHeaderParserTest(
                    ">YOR242C SSP2 SGDID:S000005768, Chr XV from 789857-788742, reverse complement, Verified ORF, \"Sporulation specific protein that localizes to the spore wall; required for sporulation at a point after meiosis II and during spore wall formation; SSP2 expression is induced midway in meiosis\"",
                    new[]
                    {
                        new FastaHeaderReaderResult(accession: "Q08646", name: "YOR242C",
                            preferredname: "SSP2_YEAST",
                            description:
                                "SSP2 SGDID:S000005768, Chr XV from 789857-788742, reverse complement, Verified ORF, \"Sporulation specific protein that localizes to the spore wall; required for sporulation at a point after meiosis II and during spore wall formation; SSP2 expression is induced midway in meiosis\"",
                            species: "Saccharomyces cerevisiae (strain ATCC 204508 / S288c) (Baker's yeast)", gene: "SSP2 YOR242C O5251")
                    }),
                new FastaHeaderParserTest(
                    ">F26D10.3	CE09682 WBGene00002005 locus:hsp-1 HSP-1 heat shock 70kd protein A status:Confirmed SW:P09446 protein_id:CAB02319.1",
                    new[]
                    {
                        new FastaHeaderReaderResult(name: "F26D10.3", accession: "P09446",
                            preferredname: "HSP7A_CAEEL",
                            description:
                                "CE09682 WBGene00002005 locus:hsp-1 HSP-1 heat shock 70kd protein A status:Confirmed SW:P09446 protein_id:CAB02319.1",
                            species: "Caenorhabditis elegans", gene: "hsp-1 hsp70a F26D10.3")
                    }),
                new FastaHeaderParserTest(
                    ">UniRef100_A5DI11 Elongation factor 2 n=1 Tax=Pichia guilliermondii RepID=EF2_PICGU",
                    new[]
                    {
                        new FastaHeaderReaderResult(name: "UniRef100_A5DI11", accession: "A5DI11",
                            preferredname: "EF2_PICGU",
                            description: "Elongation factor 2 n=1 Tax=Pichia guilliermondii RepID=EF2_PICGU",
                            species: "Meyerozyma guilliermondii (strain ATCC 6260 / CBS 566 / DSM 6381 / JCM 1539 / NBRC 10279 / NRRL Y-324) (Yeast) (Candida guilliermondii)", 
                            gene: "EFT2 PGUG_02912")
                    }),

                new FastaHeaderParserTest(
                    @">gi|"+WebEnabledFastaImporter.KNOWNGOOD_GENINFO_SEARCH_TARGET+"|30S_ribosomal_sub gi|15834432|ref|NP_313205.1| 30S ribosomal subunit protein S18 [Escherichia coli O157:H7]",
                    new[]
                    {
                        new FastaHeaderReaderResult(accession: "P0A7T9",
                            name: "gi|15834432|30S_ribosomal_sub", preferredname: "RS18_ECO57",
                            description:
                                "gi|15834432|ref|NP_313205.1| 30S ribosomal subunit protein S18 [Escherichia coli O157:H7]",
                            species: "Escherichia coli O157:H7", gene: "rpsR Z5811 ECs5178")
                    }),

                new FastaHeaderParserTest(
                    @">NP_313205 gi|15834432|ref|NP_313205.1| 30S ribosomal subunit protein S18 [Escherichia coli O157:H7]",
                    new[]
                    {
                        new FastaHeaderReaderResult(accession: "P0A7T9",
                            name: "NP_313205", preferredname: "RS18_ECO57",
                            description:
                                "gi|15834432|ref|NP_313205.1| 30S ribosomal subunit protein S18 [Escherichia coli O157:H7]",
                            species: "Escherichia coli O157:H7", gene: "rpsR Z5811 ECs5178")
                    }),

                new FastaHeaderParserTest(
                    @">sp|P01222|TSHB_HUMAN Thyrotropin subunit beta OS=Homo sapiens GN=TSHB PE=1 SV=2",
                    new[]
                    {
                        new FastaHeaderReaderResult(accession: "P01222", name: "sp|P01222|TSHB_HUMAN",
                            preferredname: "TSHB_HUMAN",
                            description: "Thyrotropin subunit beta OS=Homo sapiens GN=TSHB PE=1 SV=2",
                            species: "Homo sapiens", gene: "TSHB")
                    }),
                new FastaHeaderParserTest(
                    ">Y62E10A.1	CE22694 WBGene00004410 locus:rpa-2 status:Confirmed TR:Q9U1X9 protein_id:CAB60595.1",
                    new[]
                    {
                        new FastaHeaderReaderResult( 
                            name:"Y62E10A.1", accession:"Q9U1X9", preferredname:"Q9U1X9_CAEEL", 
                            description:"CE22694 WBGene00004410 locus:rpa-2 status:Confirmed TR:Q9U1X9 protein_id:CAB60595.1", 
                            gene:"rla-2 CELE_Y62E10A.1 Y62E10A.1", species:"Caenorhabditis elegans")
                    }),
                new FastaHeaderParserTest(">CGI_10000780",
                    new[]
                    {
                        new FastaHeaderReaderResult(accession: "K1QN71", name: "CGI_10000780",
                            preferredname: "K1QN71_CRAGI",
                            description: "Uncharacterized protein", species: "Crassostrea gigas (Pacific oyster) (Crassostrea angulata)", gene: "CGI_10000780")
                    }),
                new FastaHeaderParserTest(
                    ">ENSMUSP00000100344 pep:known chromosome:GRCm38:14:52427928:52428874:1 gene:ENSMUSG00000076758 transcript:ENSMUST00000103567 gene_biotype:TR_V_gene transcript_biotype:TR_V_gene",
                    new[]
                    {
                        new FastaHeaderReaderResult(accession: "Q5R1J0", name: "ENSMUSP00000100344",
                            preferredname: "Q5R1J0_MOUSE",
                            description:
                                "pep:known chromosome:GRCm38:14:52427928:52428874:1 gene:ENSMUSG00000076758 transcript:ENSMUST00000103567 gene_biotype:TR_V_gene transcript_biotype:TR_V_gene",
                            species: "Mus musculus (Mouse)", gene: "TRAV1")
                    }),

                new FastaHeaderParserTest(">AARS.IPI00027442 IPI:IPI00027442.4|SWISS-PROT:P49588|ENSEMBL:ENSP00000261772|REFSEQ:NP_001596|H-INV:HIT000035254|VEGA:OTTHUMP00000080084 Tax_Id=9606 Gene_Symbol=AARS Alanyl-tRNA synthetase, cytoplasmic",
                    new[]
                    {
                        new FastaHeaderReaderResult(accession: "P49588", name: "AARS.IPI00027442",
                            preferredname: "SYAC_HUMAN",
                            description: "IPI:IPI00027442.4|SWISS-PROT:P49588|ENSEMBL:ENSP00000261772|REFSEQ:NP_001596|H-INV:HIT000035254|VEGA:OTTHUMP00000080084 Tax_Id=9606 Gene_Symbol=AARS Alanyl-tRNA synthetase, cytoplasmic", 
                            species: "Homo sapiens (Human)", gene: "AARS")
                    }),

                new FastaHeaderParserTest(
                    // they may not show in the IDE but there are SOH (ASCII 0x001) characters in here
                    @">gi|15834432|30S_ribosomal_sub| gi|15834432|ref|NP_313205.1| 30S ribosomal subunit protein S18 [Escherichia coli O157:H7]gi|16132024|ref|NP_418623.1| 30S ribosomal subunit protein S18 [Escherichia coli K12]gi|16763210|ref|NP_458827.1| 30s ribosomal subunit protein S18 [Salmonella enterica subsp. enterica serovar Typhi]gi|24115555|ref|NP_710065.1| 30S ribosomal subunit protein S18 [Shigella flexneri 2a str. 301]gi|26251099|ref|NP_757139.1| 30S ribosomal protein S18 [Escherichia coli CFT073]gi|29144689|ref|NP_808031.1| 30s ribosomal subunit protein S18 [Salmonella enterica subsp. enterica serovar Typhi Ty2]gi|30065573|ref|NP_839744.1| 30S ribosomal subunit protein S18 [Shigella flexneri 2a str. 2457T]gi|133836|sp|P02374|RS18_ECOLI 30S ribosomal protein S18gi|2144767|pir||R3EC18 ribosomal protein S18 [validated] - Escherichia coli (strain K-12)gi|25294828|pir||AI1052 30s ribosomal chain protein S18 [imported] - Salmonella enterica subsp. enterica serovar Typhi (strain CT18)gi|25294838|pir||B91276 30S ribosomal subunit protein S18 [imported] - Escherichia coli (strain O157:H7, substrain RIMD 0509952)gi|42847|emb|CAA27654.1| unnamed protein product [Escherichia coli]gi|537043|gb|AAA97098.1| 30S ribosomal subunit protein S18 [Escherichia coli]gi|1790646|gb|AAC77159.1| 30S ribosomal subunit protein S18 [Escherichia coli K12]gi|13364655|dbj|BAB38601.1| 30S ribosomal subunit protein S18 [Escherichia coli O157:H7]gi|16505518|emb|CAD06870.1| 30s ribosomal subunit protein S18 [Salmonella enterica subsp. enterica serovar Typhi]gi|24054886|gb|AAN45772.1|AE015442_2 30S ribosomal subunit protein S18 [Shigella flexneri 2a str. 301]gi|26111531|gb|AAN83713.1|AE016771_224 30S ribosomal protein S18 [Escherichia coli CFT073]gi|29140328|gb|AAO71891.1| 30s ribosomal subunit protein S18 [Salmonella enterica subsp. enterica serovar Typhi Ty2]gi|30043837|gb|AAP19556.1| 30S ribosomal subunit protein S18 [Shigella flexneri 2a str. 2457T] [MASS=8986]",
                    new[]
                    {
                        new FastaHeaderReaderResult(
                            name:"gi|15834432|30S_ribosomal_sub|", accession:"P0A7T9", preferredname:"RS18_ECO57", description:"gi|15834432|ref|NP_313205.1| 30S ribosomal subunit protein S18 [Escherichia coli O157:H7]", gene:"rpsR Z5811 ECs5178", species:"Escherichia coli O157:H7"),
                        new FastaHeaderReaderResult(
                            name:"gi|16132024|ref|NP_418623.1|", accession:"P0A7T7", preferredname:"RS18_ECOLI", description:"30S ribosomal subunit protein S18 [Escherichia coli K12]", gene:"rpsR b4202 JW4160", species:"Escherichia coli (strain K12)"),
                        new FastaHeaderReaderResult(
                            name:"gi|16763210|ref|NP_458827.1|", accession:"P0A7U1", preferredname:"RS18_SALTI", description:"30s ribosomal subunit protein S18 [Salmonella enterica subsp. enterica serovar Typhi]", gene:"rpsR STY4749 t4444", species:"Salmonella typhi"),
                        new FastaHeaderReaderResult(
                            name:"gi|24115555|ref|NP_710065.1|", accession:"P0A7U2", preferredname:"RS18_SHIFL", description:"30S ribosomal subunit protein S18 [Shigella flexneri 2a str. 301]", gene:"rpsR SF4355 S4627", species:"Shigella flexneri"),
                        new FastaHeaderReaderResult(
                            name:"gi|26251099|ref|NP_757139.1|", accession:"P0A7T8", preferredname:"RS18_ECOL6", description:"30S ribosomal protein S18 [Escherichia coli CFT073]", gene:"rpsR c5292", species:"Escherichia coli O6:H1 (strain CFT073 / ATCC 700928 / UPEC)"),
                        new FastaHeaderReaderResult(
                            name:"gi|29144689|ref|NP_808031.1|", accession:"P0A7U1", preferredname:"RS18_SALTI", description:"30s ribosomal subunit protein S18 [Salmonella enterica subsp. enterica serovar Typhi Ty2]", gene:"rpsR STY4749 t4444", species:"Salmonella typhi"),
                        new FastaHeaderReaderResult(
                            name:"gi|30065573|ref|NP_839744.1|", accession:"P0A7U2", preferredname:"RS18_SHIFL", description:"30S ribosomal subunit protein S18 [Shigella flexneri 2a str. 2457T]", gene:"rpsR SF4355 S4627", species:"Shigella flexneri"),
                        new FastaHeaderReaderResult(
                            name:"gi|133836|sp|P02374|RS18_ECOLI", accession:"P0A7T7", preferredname:"RS18_ECOLI", description:"30S ribosomal protein S18", gene:"rpsR b4202 JW4160", species:"Escherichia coli (strain K12)"),
                        new FastaHeaderReaderResult(
                            name:"gi|2144767|pir||R3EC18", accession:"P0A7T9", preferredname:"RS18_ECO57", description:"ribosomal protein S18 [validated] - Escherichia coli (strain K-12)", gene:"rpsR Z5811 ECs5178", species:"Escherichia coli O157:H7"),
                        new FastaHeaderReaderResult( // entrez gives AI1052 as accession from gi|25294828 but UniprotKB doesn't recognize that accession, so no gene or species
                            name:"gi|25294828|pir||AI1052", accession:"AI1052", preferredname:"pir||AI1052", description:"30s ribosomal chain protein S18 [imported] - Salmonella enterica subsp. enterica serovar Typhi (strain CT18)", gene:novalue, species:novalue),
                        new FastaHeaderReaderResult(
                            name:"gi|25294838|pir||B91276", accession:"P0A7T9", preferredname:"RS18_ECO57", description:"30S ribosomal subunit protein S18 [imported] - Escherichia coli (strain O157:H7, substrain RIMD 0509952)",  species: "Escherichia coli O157:H7", gene: "rpsR Z5811 ECs5178"),
                        new FastaHeaderReaderResult(
                            name:"gi|42847|emb|CAA27654.1|", accession:"P0A7T7", preferredname:"RS18_ECOLI", description:"unnamed protein product [Escherichia coli]", gene:"rpsR b4202 JW4160", species:"Escherichia coli (strain K12)"),
                        new FastaHeaderReaderResult(
                            name:"gi|537043|gb|AAA97098.1|", accession:"P0A7T7", preferredname:"RS18_ECOLI", description:"30S ribosomal subunit protein S18 [Escherichia coli]", gene:"rpsR b4202 JW4160", species:"Escherichia coli (strain K12)"),
                        new FastaHeaderReaderResult(
                            name:"gi|1790646|gb|AAC77159.1|", accession:"P0A7T7", preferredname:"RS18_ECOLI", description:"30S ribosomal subunit protein S18 [Escherichia coli K12]", gene:"rpsR b4202 JW4160", species:"Escherichia coli (strain K12)"),
                        new FastaHeaderReaderResult(
                            name:"gi|13364655|dbj|BAB38601.1|", accession:"P0A7T9", preferredname:"RS18_ECO57", description:"30S ribosomal subunit protein S18 [Escherichia coli O157:H7]", gene:"rpsR Z5811 ECs5178", species:"Escherichia coli O157:H7"),
                        new FastaHeaderReaderResult(
                            name:"gi|16505518|emb|CAD06870.1|", accession:"P0A7U1", preferredname:"RS18_SALTI", description:"30s ribosomal subunit protein S18 [Salmonella enterica subsp. enterica serovar Typhi]", gene:"rpsR STY4749 t4444", species:"Salmonella typhi"),
                        new FastaHeaderReaderResult(
                            name:"gi|24054886|gb|AAN45772.1|AE015442_2", accession:"P0A7U2", preferredname:"RS18_SHIFL", description:"30S ribosomal subunit protein S18 [Shigella flexneri 2a str. 301]", gene:"rpsR SF4355 S4627", species:"Shigella flexneri"),
                        new FastaHeaderReaderResult(
                            name:"gi|26111531|gb|AAN83713.1|AE016771_224", accession:"P0A7T8", preferredname:"RS18_ECOL6", description:"30S ribosomal protein S18 [Escherichia coli CFT073]", gene:"rpsR c5292", species:"Escherichia coli O6:H1 (strain CFT073 / ATCC 700928 / UPEC)"),
                        new FastaHeaderReaderResult(
                            name:"gi|29140328|gb|AAO71891.1|", accession:"P0A7U1", preferredname:"RS18_SALTI", description:"30s ribosomal subunit protein S18 [Salmonella enterica subsp. enterica serovar Typhi Ty2]", gene:"rpsR STY4749 t4444", species:"Salmonella typhi"),
                        new FastaHeaderReaderResult(
                            name:"gi|30043837|gb|AAP19556.1|", accession:"P0A7U2", preferredname:"RS18_SHIFL", description:"30S ribosomal subunit protein S18 [Shigella flexneri 2a str. 2457T] [MASS=8986]", gene:"rpsR SF4355 S4627", species:"Shigella flexneri")
                    }),

                // keep this one negative test here in the middle, it ensures more code coverage in the retry code
                new FastaHeaderParserTest(">scoogly doodly abeebopboom",
                    new[]
                    {
                        new FastaHeaderReaderResult(accession: novalue, name: "scoogly", preferredname: novalue,
                            description: "doodly abeebopboom", species: novalue, gene: novalue)
                    }),
                new FastaHeaderParserTest(
                    ">AAS51520 pep:known chromosome:ASM9102v1:IV:2278:3450:1 gene:AGOS_ADL400W transcript:AAS51520 description:\"ADL400WpAFR758Cp\"",
                    new[]
                    {
                        new FastaHeaderReaderResult(accession: "Q75BG2", name: "AAS51520",
                            preferredname: "Q75BG2_ASHGO",
                            description:
                                "pep:known chromosome:ASM9102v1:IV:2278:3450:1 gene:AGOS_ADL400W transcript:AAS51520 description:\"ADL400WpAFR758Cp\"",
                            species: "Ashbya gossypii (strain ATCC 10895 / CBS 109.51 / FGSC 9923 / NRRL Y-1056) (Yeast) (Eremothecium gossypii)",
                            gene: "ADL400W AGOS_ADL400W AGOS_AFR758C")
                    }),

               // keep these negative tests at end, it ensures more code coverage in the retry code
                new FastaHeaderParserTest( // this one is a negative test
                    NEGTEST,
                    new[]
                    {
                        // no, this is not the right answer - it's a negative test
                        new FastaHeaderReaderResult(accession: "Happymeal", preferredname: "fish",
                            name: "grackle", description: "cat",
                            species: "Cleveland", gene: "France")
                    }),

                new FastaHeaderParserTest( // failure is expected with uniprot service
                    ">CGI_99999999", // no such thing
                    new[]
                    {
                        new FastaHeaderReaderResult(accession: novalue, name: "CGI_99999999",
                            preferredname: novalue,
                            description: novalue, species: novalue, gene: novalue, websearchcode: WebEnabledFastaImporter.SEARCHDONE_TAG)
                    }),

                new FastaHeaderParserTest( // doesn't exist, lookup failure is expected
                    ">ENSMUSP99999999999 pep:known chromosome:GRCm83:14:52742928:52824874:1 gene:ENSMUSG99999999999999 transcript:ENSMUST9999999999999999 gene_biotype:TR_Z_gene transcript_biotype:TR_Z_gene",
                    new[]
                    {
                        new FastaHeaderReaderResult(accession: novalue,
                            name: "ENSMUSP99999999999", preferredname: novalue,
                            description:
                                "pep:known chromosome:GRCm83:14:52742928:52824874:1 gene:ENSMUSG99999999999999 transcript:ENSMUST9999999999999999 gene_biotype:TR_Z_gene transcript_biotype:TR_Z_gene",
                            species: novalue, gene: novalue, websearchcode: WebEnabledFastaImporter.SEARCHDONE_TAG)
                    }),
               // keep these negative tests at end, it ensures more code coverage in the retry code
            };
        }

        /// <summary>
        /// for testing without requiring web access - returns the expected web responses for the tests herein.
        /// </summary>
        public class PlaybackProvider : WebEnabledFastaImporter.WebSearchProvider
        {
            public override XmlTextReader GetXmlTextReader(string url)
            {
                // should look something like "http://eutils.ncbi.nlm.nih.gov/entrez/eutils/efetch.fcgi?db=protein&id=15834432,15834432&tool=%22skyline%22&email=%22johnqdeveloper@proteinms.net%22&retmode=xml"
                var searches = url.Split('=')[2].Split('&')[0].Split(',');
                var sb = new StringBuilder();
                if (url.Contains(".gov")) // watch for deliberately malformed url in tests Not L10N
                {
                    if (url.Contains("rettype=docsum"))
                    {
                        sb.Append("<?xml version=\"1.0\"?>\n<eSummaryResult>\n"); // Not L10N
                        foreach (var search in searches)
                        {
                            var test = FindTest(search);
                            if ((null != test) && !String.IsNullOrEmpty(test.Protein.Accession))
                            {
                                var intermediateSearchTerm = test.GetIntermediateSearchterm(search);
                                sb.AppendFormat("<Id>{0}</Id>",search);
                                sb.AppendFormat("<DocSum> ");
                                sb.AppendFormat("<Item Name=\"Caption\" Type=\"String\">{0}</Item>",
                                    intermediateSearchTerm);
                                sb.AppendFormat("<Item Name=\"ReplacedBy\" Type=\"String\">{0}</Item>",
                                    intermediateSearchTerm);
                                sb.AppendFormat("</DocSum>\n"); // Not L10N
                            }
                        }
                        sb.AppendFormat("</eSummaryResult>\n"); // Not L10N
                    }
                    else
                    {
                        sb.Append("<?xml version=\"1.0\"?>\n<GBSet>\n"); // Not L10N
                        foreach (var search in searches)
                        {
                            var test = FindTest(search);
                            if ((null != test) && !String.IsNullOrEmpty(test.Protein.Accession))
                            {
                                sb.AppendFormat("<GBSeq> ");
                                if (test.Protein.PreferredName != null)
                                    sb.AppendFormat("<GBSeq_locus>{0}</GBSeq_locus>", test.Protein.PreferredName);
                                        // Not L10N
                                if (test.Protein.Description != null)
                                    sb.AppendFormat(" <GBSeq_definition>{0}</GBSeq_definition> ",
                                        test.Protein.Description); // Not L10N
                                if (test.Protein.Accession != null)
                                    sb.AppendFormat("<GBSeq_primary-accession>{0}</GBSeq_primary-accession>",
                                        test.Protein.Accession); // Not L10N 
                                if (test.Protein.Species != null)
                                    sb.AppendFormat("<GBSeq_organism>{0}</GBSeq_organism> ", test.Protein.Species);
                                        // Not L10N
                                if (test.Protein.Gene != null)
                                    sb.AppendFormat(
                                        "<GBQualifier> <GBQualifier_name>gene</GBQualifier_name> <GBQualifier_value>{0}</GBQualifier_value> </GBQualifier> ",
                                        test.Protein.Gene); // Not L10N
                                sb.AppendFormat("</GBSeq>\n"); // Not L10N
                            }
                        }
                        sb.Append("</GBSet>"); // Not L10N
                    }
                    return new XmlTextReader(MakeStream(sb));
                }
                else
                {
                    throw new WebException("error 404"); // mimic bad url behavior Not L10N
                }
            }

            public override Stream GetWebResponseStream(string url, int timeout)
            {
                // should look something like "http://www.uniprot.xyzpdq/uniprot/?query=reviewed:yes+AND+(P04638+OR+SGD:S000005768+OR+CAB02319.1)&format=tab&columns=id,entry name,protein names,genes,organism"
                var searches = url.Split('(')[1].Split(')')[0].Split('+').Where(s => !Equals(s, "OR")).ToArray();
                var sb = new StringBuilder();
                if (url.Contains(".org")) // watch for deliberately malformed url in tests Not L10N
                {
                    sb.Append("Entry\tEntry name\tProtein names\tGene names\tOrganism\n");
                    foreach (var search in searches)
                    {
                        var test = FindTest(search);
                        if ((null != test) && !String.IsNullOrEmpty(test.Protein.Accession))
                        {
                            sb.AppendFormat(
                                "{0}\t{1}\t{2}\t{3}\t{4}\n", // Not L10N
                                test.Protein.Accession , test.Protein.PreferredName ?? String.Empty,
                                test.Protein.Description ?? String.Empty, test.Protein.Gene ?? String.Empty, test.Protein.Species ?? String.Empty);
                        }
                    }
                    return MakeStream(sb);
                }
                else
                {
                    throw new WebException("error 404"); //  mimic bad url behavior Not L10N
                }
            }

            private Stream MakeStream(StringBuilder sb)
            {
                MemoryStream stream = new MemoryStream();
                StreamWriter writer = new StreamWriter(stream);
                writer.Write(sb.ToString());
                writer.Flush();
                stream.Position = 0;
                return stream;
            }

            private FastaHeaderReaderResult FindTest(string keyword)
            {
                if (!string.IsNullOrEmpty(keyword))
                {
                    foreach (var test in GetTests())
                    {
                        foreach (var expectedResult in test.ExpectedResults)
                        {
                            if ((!String.IsNullOrEmpty(expectedResult.Protein.Accession) &&
                                    (expectedResult.Protein.Accession.ToUpperInvariant().StartsWith(keyword.ToUpperInvariant()) ||
                                    keyword.ToUpperInvariant().StartsWith(expectedResult.Protein.Accession.ToUpperInvariant()))) ||
                                (!String.IsNullOrEmpty(expectedResult.Protein.Name) &&
                                (expectedResult.Protein.Name.ToUpperInvariant().Contains(keyword.ToUpperInvariant()))))
                            {
                                return expectedResult;
                            }
                        }
                    }
                    // no joy yet - see if its buried in name or description, as in our GI->Uniprot scenario
                    keyword = keyword.Split('.')[0]; // drop .n from xp_mmmmmmm.n
                    foreach (var test in GetTests())
                    {
                        foreach (var expectedResult in test.ExpectedResults)
                        {
                            if (Equals(keyword,FastaHeaderReaderResult.FindTerm(expectedResult.Protein, @"ref\|")) ||
                                Equals(keyword,FastaHeaderReaderResult.FindTerm(expectedResult.Protein, @"gi\|")))
                            {
                                return expectedResult;
                            }
                        }
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// like the actual  WebEnabledFastaImporter.WebSearchProvider,
        /// but intentionally generates bad URLs to test error handling
        /// </summary>
        public class DoomedWebSearchProvider : WebEnabledFastaImporter.WebSearchProvider
        {
            public override int GetTimeoutMsec(int searchTermCount)
            {
                return 10 * (10 + (searchTermCount / 5));
            }

            public override int WebRetryCount()
            {
                return 1; // once is plenty
            }

            public override string ConstructEntrezURL(IEnumerable<string> searches, bool summary)
            {
                var result = base.ConstructEntrezURL(searches, summary).Replace("nlm.nih.gov", "nlm.nih.gummint"); // provoke a failure for test purposes Not L10N
                return result;
            }

            public override string ConstructUniprotURL(IEnumerable<string> searches, bool reviewedOnly)
            {
                var result = base.ConstructUniprotURL(searches, reviewedOnly).Replace("uniprot.org", "uniprot.xyzpdq"); // provoke a failure for test purposes Not L10N
                return result;
            }

        }

        const string EATDEADEELS = "EATDEADEELS";
        
        /// <summary>
        /// Test the basic parsing, no attempt at protein metadata resolution
        /// </summary>
        [TestMethod]
        public void TestBasicFastaImport()
        {
            List<FastaHeaderParserTest> tests = GetTests();
            var dbProteins = new List<DbProtein>();
            var dbProteinNames = new List<DbProteinName>();
            WebEnabledFastaImporter fastaImporter = new WebEnabledFastaImporter(new WebEnabledFastaImporter.FakeWebSearchProvider());
            int fakeID = 0;
            foreach (var dbProtein in fastaImporter.Import(new StringReader(GetFastaTestText())))
            {
                dbProtein.Id = fakeID++;
                foreach (var name in dbProtein.Names)
                {
                    name.Id = fakeID++;
                    dbProteinNames.Add(name);
                }
                dbProteins.Add(dbProtein);
            }
            foreach (var dbProtein in dbProteins)
            {
                int testnum = (dbProtein.Sequence.Length / EATDEADEELS.Length) - 1;
                Assert.AreEqual(dbProtein.Names.Count, tests[testnum].ExpectedResults.Length);
                int n = 0;
                foreach (var name in dbProtein.Names)
                {
                    var actual = new DbProteinName(null, name.GetProteinMetadata());
                    var expected = new DbProteinName(null, tests[testnum].ExpectedResults[n++].Protein);
                    if (NEGTEST == tests[testnum].Header)
                    {
                        Assert.AreNotEqual(expected.Name, actual.Name);
                        Assert.AreNotEqual(expected.Description, actual.Description);
                    }
                    else
                    {
                        Assert.AreEqual(expected.Name, actual.Name);
                        if (actual.Description!=null)
                            Assert.AreEqual(expected.Description, actual.Description);
                    }
                }
            }
        }

        public static string GetFastaTestText(int maxEntries = -1)
        {
            var fastaLines = new StringBuilder();
            int testnum = 0;
            var tests = GetTests();
            foreach (var t in tests)
            {
                fastaLines.Append(t.Header);
                fastaLines.Append("\n");
                for (int mm = testnum++; mm >= 0; mm--)
                    fastaLines.Append(EATDEADEELS + "\n");
                if ((maxEntries >= 0) && (testnum >= maxEntries))
                    break;
            }
            return fastaLines.ToString();
        }


        [TestMethod]
        public void TestFastaImport()
        {
            DoTestFastaImport(false);  // run with simulated web access
        }

        [TestMethod]
        public void WebTestFastaImport()  
        {
            if (AllowInternetAccess) // Only run this if SkylineTester has enabled web access
            {
                DoTestFastaImport(true); // run with actual web access
            }
        }


        public void DoTestFastaImport(bool useActualWebAcess) // call with true from perf test
        {

            var fastaLines = new StringBuilder();
            int testnum = 0;
            const string deadeels = "EATDEADEELS";
            var tests = GetTests();
            foreach (var t in tests)
            {
                fastaLines.Append(t.Header);
                fastaLines.Append("\n");
                for (int mm = testnum++; mm >= 0; mm--)
                    fastaLines.Append(deadeels + "\n");
            }

            var dbProteins = new List<DbProtein>();
            var dbProteinNames = new List<DbProteinName>();
            WebEnabledFastaImporter fastaImporter = new WebEnabledFastaImporter(new WebEnabledFastaImporter.DelayedWebSearchProvider());
            int fakeID = 0;
            foreach (var dbProtein in fastaImporter.Import(new StringReader(fastaLines.ToString())))
            {
                dbProtein.Id = fakeID++;
                foreach (var name in dbProtein.Names)
                {
                    name.Id = fakeID++;
                    dbProteinNames.Add(name);
                }
                dbProteins.Add(dbProtein);
            }

            

            for (int test = 2; test-- > 0;)
            {
                if (test == 1) // first, test poor internet access
                    fastaImporter = new WebEnabledFastaImporter(new DoomedWebSearchProvider()); // intentionally messes up the URLs
                else  // then test web search code - either live in a perf test, or using playback object
                    fastaImporter = new WebEnabledFastaImporter(useActualWebAcess? new WebEnabledFastaImporter.WebSearchProvider() : new PlaybackProvider());
                var results = fastaImporter.DoWebserviceLookup(dbProteinNames, null, false).ToList(); // No progress moniotr, and don't be polite get it all at once
                foreach (var result in results)
                {
                    if (result != null)
                    {
                        bool searchCompleted =
                            String.IsNullOrEmpty(result.GetProteinMetadata().GetPendingSearchTerm());
                        bool searchDelayed = (test==1); // first go round we simulate bad web access
                        if (!Equals(result.WebSearchInfo.ToString(), WebEnabledFastaImporter.SEARCHDONE_TAG.ToString(CultureInfo.InvariantCulture))) // the 'no search possible' case
                            Assert.IsTrue(searchCompleted == !searchDelayed);
                    }
                }
            }
            Assert.AreEqual(tests.Count, dbProteins.Count);


            var errStringE = String.Empty;
            var errStringA = String.Empty;
            foreach (var dbProtein in dbProteins)
            {
                // note that fastaImporter doesn't always present proteins in file order, due to 
                // batching webserver lookups - but we can discern the test number by the
                // goofy sequence we created
                testnum = (dbProtein.Sequence.Length/deadeels.Length) - 1;
                Assert.AreEqual(dbProtein.Names.Count, tests[testnum].ExpectedResults.Length);
                int n = 0;
                var errors = new List<Tuple<String, String>>();
                foreach (var name in dbProtein.Names)
                {
                    var actual = new DbProteinName(null, name.GetProteinMetadata());
                    var expected = new DbProteinName(null, tests[testnum].ExpectedResults[n++].Protein);

                    actual.ClearWebSearchInfo();
                    expected.ClearWebSearchInfo(); // this is not a comparison we care about

                    if (NEGTEST == tests[testnum].Header)
                    {
                        if (Equals(expected.GetProteinMetadata(), actual.GetProteinMetadata()))
                            // negtest should fail
                            errors.Add(new Tuple<string, string>(expected.GetProteinMetadata().ToString(),
                                actual.GetProteinMetadata().ToString()));
                    }
                    else
                    {
                        if (!Equals(expected.GetProteinMetadata(), actual.GetProteinMetadata()))
                            errors.Add(new Tuple<string, string>(expected.GetProteinMetadata().ToString(),
                                actual.GetProteinMetadata().ToString()));
                    }
                }
                foreach (var e in errors)
                {
                    errStringE += "\n" + e.Item1;
                    errStringA += "\n" + e.Item2;
                }
            }
            Assert.AreEqual(errStringE + "\n", errStringA + "\n");
        }
    }
}
