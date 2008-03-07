//
// cv.hpp
//
//
// Darren Kessner <Darren.Kessner@cshs.org>
//
// Copyright 2007 Spielberg Family Center for Applied Proteomics
//   Cedars-Sinai Medical Center, Los Angeles, California  90048
//   Unauthorized use or reproduction prohibited
//
// This file was generated by cvgen.
//


#ifndef _CV_HPP_
#define _CV_HPP_


#include <string>
#include <vector>


// [psi-ms.obo]
//   format-version: 1.0
//   date: 21:02:2008 10:40
//   saved-by: edeutsch
//   auto-generated-by: OBO-Edit 1.101
//   default-namespace: PSI-MS
//   remark: When appropriate the definition and synonyms of a term are reported exactly as in the chapter 12 of IUPAC orange book. See http://www.iupac.org/projects/2003/2003-056-2-500.html.
//


namespace pwiz {
namespace msdata {


/// enumeration of controlled vocabulary (CV) terms, generated from an OBO file
enum CVID
{
    CVID_Unknown = -1,
    MS_Mass_Spectrometry_Controlled_Vocabulary = 0,
    MS_sample_number = 1000001,
    MS_sample_name = 1000002,
    MS_sample_state = 1000003,
    MS_sample_mass = 1000004,
    MS_sample_volume = 1000005,
    MS_sample_concentration = 1000006,
    MS_inlet_type = 1000007,
    MS_ionization_type = 1000008,
    MS_analyzer_type = 1000010,
    MS_mass_resolution = 1000011,
    MS_resolution_measurement_method = 1000012,
    MS_resolution_type = 1000013,
    MS_accuracy = 1000014,
    MS_scan_rate = 1000015,
    MS_scan_time = 1000016,
    MS_Scan_Function = 1000017,
    MS_scan_direction = 1000018,
    MS_scan_law = 1000019,
    MS_scanning_method = 1000020,
    MS_reflectron_state = 1000021,
    MS_TOF_Total_Path_Length = 1000022,
    MS_isolation_width = 1000023,
    MS_final_MS_exponent = 1000024,
    MS_magnetic_field_strength = 1000025,
    MS_B = MS_magnetic_field_strength,
    MS_detector_type = 1000026,
    MS_detector_acquisition_mode = 1000027,
    MS_detector_resolution = 1000028,
    MS_sampling_frequency = 1000029,
    MS_instrument_model = 1000031,
    MS_customization = 1000032,
    MS_deisotoping = 1000033,
    MS_charge_deconvolution = 1000034,
    MS_peak_picking = 1000035,
    MS_polarity = 1000037,
    MS_minute = 1000038,
    MS_second = 1000039,
    MS_m_z = 1000040,
    MS_Mass_To_Charge_Ratio = MS_m_z,
    MS_charge_state = 1000041,
    MS_intensity = 1000042,
    MS_intensity_unit = 1000043,
    MS_dissociation_method = 1000044,
    MS_collision_energy = 1000045,
    MS_energy_unit = 1000046,
    MS_emulsion = 1000047,
    MS_gas = 1000048,
    MS_liquid = 1000049,
    MS_solid = 1000050,
    MS_solution = 1000051,
    MS_suspension = 1000052,
    MS_sample_batch = 1000053,
    MS_chromatography = 1000054,
    MS_continuous_flow_fast_atom_bombardment = 1000055,
    MS_CF_FAB = MS_continuous_flow_fast_atom_bombardment,
    MS_direct_inlet = 1000056,
    MS_electrospray_inlet = 1000057,
    MS_flow_injection_analysis = 1000058,
    MS_inductively_coupled_plasma = 1000059,
    MS_infusion = 1000060,
    MS_jet_separator = 1000061,
    MS_membrane_separator = 1000062,
    MS_moving_belt = 1000063,
    MS_moving_wire = 1000064,
    MS_open_split = 1000065,
    MS_particle_beam = 1000066,
    MS_reservoir = 1000067,
    MS_septum = 1000068,
    MS_thermospray_inlet = 1000069,
    MS_atmospheric_pressure_chemical_ionization = 1000070,
    MS_APCI = MS_atmospheric_pressure_chemical_ionization,
    MS_chemical_ionization = 1000071,
    MS_CI = MS_chemical_ionization,
    MS_electrospray_ionization = 1000073,
    MS_ESI = MS_electrospray_ionization,
    MS_fast_atom_bombardment_ionization = 1000074,
    MS_FAB = MS_fast_atom_bombardment_ionization,
    MS_matrix_assisted_laser_desorption_ionization = 1000075,
    MS_MALDI = MS_matrix_assisted_laser_desorption_ionization,
    MS_axial_ejection_linear_ion_trap = 1000078,
    MS_fourier_transform_ion_cyclotron_resonance_mass_spectrometer = 1000079,
    MS_FT_ICR = MS_fourier_transform_ion_cyclotron_resonance_mass_spectrometer,
    MS_magnetic_sector = 1000080,
    MS_quadrupole = 1000081,
    MS_quadrupole_ion_trap = 1000082,
    MS_Paul_Ion_trap = MS_quadrupole_ion_trap,
    MS_QIT = MS_quadrupole_ion_trap,
    MS_Quistor = MS_quadrupole_ion_trap,
    MS_radial_ejection_linear_ion_trap = 1000083,
    MS_time_of_flight = 1000084,
    MS_TOF = MS_time_of_flight,
    MS_baseline = 1000085,
    MS_full_width_at_half_maximum = 1000086,
    MS_FWHM = MS_full_width_at_half_maximum,
    MS_ten_percent_valley = 1000087,
    MS_constant = 1000088,
    MS_proportional = 1000089,
    MS_mass_scan = 1000090,
    MS_selected_ion_detection = 1000091,
    MS_decreasing_m_z_scan = 1000092,
    MS_increasing_m_z_scan = 1000093,
    MS_exponential = 1000094,
    MS_linear = 1000095,
    MS_quadratic = 1000096,
    MS_constant_neutral_mass_loss = 1000097,
    MS_multiple_ion_monitoring = 1000098,
    MS_precursor_ion_scan = 1000100,
    MS_product_ion_scan = 1000101,
    MS_reflectron_off = 1000105,
    MS_reflectron_on = 1000106,
    MS_channeltron = 1000107,
    MS_Channeltron_Detector = MS_channeltron,
    MS_conversion_dynode_electron_multiplier = 1000108,
    MS_conversion_dynode_photomultiplier = 1000109,
    MS_daly_detector = 1000110,
    MS_Daly = MS_daly_detector,
    MS_electron_multiplier_tube = 1000111,
    MS_EMT = MS_electron_multiplier_tube,
    MS_faraday_cup = 1000112,
    MS_focal_plane_array = 1000113,
    MS_microchannel_plate_detector = 1000114,
    MS_multichannel_plate = MS_microchannel_plate_detector,
    MS_multi_collector = 1000115,
    MS_photomultiplier = 1000116,
    MS_PMT = MS_photomultiplier,
    MS_analog_digital_converter = 1000117,
    MS_ADC = MS_analog_digital_converter,
    MS_pulse_counting = 1000118,
    MS_time_digital_converter = 1000119,
    MS_TDC = MS_time_digital_converter,
    MS_transient_recorder = 1000120,
    MS_ABI___SCIEX_instrument_model = 1000121,
    MS_Bruker_Daltonics_instrument_model = 1000122,
    MS_IonSpec_instrument_model = 1000123,
    MS_Shimadzu_instrument_model = 1000124,
    MS_Thermo_Finnigan_instrument_model = 1000125,
    MS_Waters_instrument_model = 1000126,
    MS_centroid_mass_spectrum = 1000127,
    MS_Discrete_Mass_Spectrum = MS_centroid_mass_spectrum,
    MS_profile_mass_spectrum = 1000128,
    MS_continuous_mass_spectrum = MS_profile_mass_spectrum,
    MS_Continuum_Mass_Spectrum = MS_profile_mass_spectrum,
    MS_negative_scan = 1000129,
    MS_positive_scan = 1000130,
    MS_number_of_counts = 1000131,
    MS_percent_of_base_peak = 1000132,
    MS_collision_induced_dissociation = 1000133,
    MS_CID = MS_collision_induced_dissociation,
    MS_plasma_desorption = 1000134,
    MS_PD = MS_plasma_desorption,
    MS_post_source_decay = 1000135,
    MS_PSD = MS_post_source_decay,
    MS_surface_induced_dissociation = 1000136,
    MS_SID = MS_surface_induced_dissociation,
    MS_electron_volt = 1000137,
    MS_eV = MS_electron_volt,
    MS_percent_collision_energy = 1000138,
    MS_4000_Q_TRAP = 1000139,
    MS_4700_Proteomic_Analyzer = 1000140,
    MS_APEX_IV = 1000141,
    MS_APEX_Q = 1000142,
    MS_API_150EX = 1000143,
    MS_API_150EX_Prep = 1000144,
    MS_API_2000 = 1000145,
    MS_API_3000 = 1000146,
    MS_API_4000 = 1000147,
    MS_autoFlex_II = 1000148,
    MS_autoFlex_TOF_TOF = 1000149,
    MS_Auto_Spec_Ultima_NT = 1000150,
    MS_Bio_TOF_II = 1000151,
    MS_Bio_TOF_Q = 1000152,
    MS_DELTA_plusAdvantage = 1000153,
    MS_DELTAplusXP = 1000154,
    MS_ELEMENT2 = 1000155,
    MS_esquire4000 = 1000156,
    MS_esquire6000 = 1000157,
    MS_explorer = 1000158,
    MS_GCT = 1000159,
    MS_HCT = 1000160,
    MS_HCT_Plus = 1000161,
    MS_HiRes_ESI = 1000162,
    MS_HiRes_MALDI = 1000163,
    MS_IsoPrime = 1000164,
    MS_IsoProbe = 1000165,
    MS_IsoProbe_T = 1000166,
    MS_LCQ_Advantage = 1000167,
    MS_LCQ_Classic = 1000168,
    MS_LCQ_Deca_XP_Plus = 1000169,
    MS_M_LDI_L = 1000170,
    MS_M_LDI_LR = 1000171,
    MS_MAT253 = 1000172,
    MS_MAT900XP = 1000173,
    MS_MAT900XP_Trap = 1000174,
    MS_MAT95XP = 1000175,
    MS_MAT95XP_Trap = 1000176,
    MS_microFlex = 1000177,
    MS_microTOFLC = 1000178,
    MS_neptune = 1000179,
    MS_NG_5400 = 1000180,
    MS_OMEGA = 1000181,
    MS_OMEGA_2001 = 1000182,
    MS_OmniFlex = 1000183,
    MS_Platform_ICP = 1000184,
    MS_PolarisQ = 1000185,
    MS_proteomics_solution_1 = 1000186,
    MS_Q_TRAP = 1000187,
    MS_q_tof_micro = 1000188,
    MS_q_tof_ultima = 1000189,
    MS_QSTAR = 1000190,
    MS_quattro_micro = 1000191,
    MS_Quattro_UItima = 1000192,
    MS_Surveyor_MSQ = 1000193,
    MS_SymBiot_I = 1000194,
    MS_SymBiot_XVI = 1000195,
    MS_TEMPUS_TOF = 1000196,
    MS_TRACE_DSQ = 1000197,
    MS_TRITON = 1000198,
    MS_TSQ_Quantum = 1000199,
    MS_ultima = 1000200,
    MS_ultraFlex = 1000201,
    MS_ultraFlex_TOF_TOF = 1000202,
    MS_Voyager_DE_PRO = 1000203,
    MS_Voyager_DE_STR = 1000204,
    MS_selected_ion_monitoring = 1000205,
    MS_Multiple_Ion_Monitoring = MS_selected_ion_monitoring,
    MS_SIM = MS_selected_ion_monitoring,
    MS_selected_reaction_monitoring = 1000206,
    MS_SRM = MS_selected_reaction_monitoring,
    MS_accurate_mass = 1000207,
    MS_average_mass = 1000208,
    MS_appearance_energy = 1000209,
    MS_AE = MS_appearance_energy,
    MS_base_peak = 1000210,
    MS_BP = MS_base_peak,
    MS_charge_number = 1000211,
    MS_z = MS_charge_number,
    MS_dalton = 1000212,
    MS_Da = MS_dalton,
    MS_electron_affinity = 1000213,
    MS_EA = MS_electron_affinity,
    MS_electron_energy_obsolete = 1000214,
    MS_exact_mass = 1000215,
    MS_field_free_region = 1000216,
    MS_FFR = MS_field_free_region,
    MS_ionization_cross_section = 1000217,
    MS_ionization_energy = 1000219,
    MS_IE = MS_ionization_energy,
    MS_isotope_dilution_mass_spectrometry = 1000220,
    MS_IDMS = MS_isotope_dilution_mass_spectrometry,
    MS_magnetic_deflection = 1000221,
    MS_mass_defect = 1000222,
    MS_mass_number = 1000223,
    MS_molecular_mass = 1000224,
    MS_monoisotopic_mass = 1000225,
    MS_molecular_beam_mass_spectrometry = 1000226,
    MS_MBMS = MS_molecular_beam_mass_spectrometry,
    MS_multiphoton_ionization = 1000227,
    MS_MPI = MS_multiphoton_ionization,
    MS_nitrogen_rule = 1000228,
    MS_nominal_mass = 1000229,
    MS_odd_electron_rule = 1000230,
    MS_peak = 1000231,
    MS_proton_affinity = 1000233,
    MS_PA = MS_proton_affinity,
    MS_mass_resolving_power = 1000234,
    MS_total_ion_chromatogram__ = 1000235,
    MS_transmission = 1000236,
    MS_unified_atomic_mass_unit = 1000237,
    MS_u = MS_unified_atomic_mass_unit,
    MS_accelerator_mass_spectrometry = 1000238,
    MS_AMS = MS_accelerator_mass_spectrometry,
    MS_atmospheric_pressure_matrix_assisted_laser_desorption_ionization = 1000239,
    MS_AP_MALDI = MS_atmospheric_pressure_matrix_assisted_laser_desorption_ionization,
    MS_atmospheric_pressure_ionization = 1000240,
    MS_API = MS_atmospheric_pressure_ionization,
    MS_blackbody_infrared_radiative_dissociation = 1000242,
    MS_BIRD = MS_blackbody_infrared_radiative_dissociation,
    MS_charge_remote_fragmentation = 1000243,
    MS_CRF = MS_charge_remote_fragmentation,
    MS_consecutive_reaction_monitoring = 1000244,
    MS_CRM = MS_consecutive_reaction_monitoring,
    MS_charge_stripping = 1000245,
    MS_CS = MS_charge_stripping,
    MS_delayed_extraction = 1000246,
    MS_DE = MS_delayed_extraction,
    MS_desorption_ionization = 1000247,
    MS_DI = MS_desorption_ionization,
    MS_direct_insertion_probe = 1000248,
    MS_DIP = MS_direct_insertion_probe,
    MS_direct_liquid_introduction = 1000249,
    MS_DLI = MS_direct_liquid_introduction,
    MS_electron_capture_dissociation = 1000250,
    MS_ECD = MS_electron_capture_dissociation,
    MS_even_electron_ion = 1000251,
    MS_EE = MS_even_electron_ion,
    MS_electron_induced_excitation_in_organics = 1000252,
    MS_EIEIO = MS_electron_induced_excitation_in_organics,
    MS_electron_multiplier = 1000253,
    MS_EM = MS_electron_multiplier,
    MS_electrostatic_energy_analyzer = 1000254,
    MS_ESA = MS_electrostatic_energy_analyzer,
    MS_flowing_afterglow = 1000255,
    MS_FA = MS_flowing_afterglow,
    MS_high_field_asymmetric_waveform_ion_mobility_spectrometry = 1000256,
    MS_FAIMS = MS_high_field_asymmetric_waveform_ion_mobility_spectrometry,
    MS_field_desorption = 1000257,
    MS_FD = MS_field_desorption,
    MS_field_ionization = 1000258,
    MS_FI = MS_field_ionization,
    MS_glow_discharge_ionization = 1000259,
    MS_GD_MS = MS_glow_discharge_ionization,
    MS_ion_kinetic_energy_spectrometry = 1000260,
    MS_IKES = MS_ion_kinetic_energy_spectrometry,
    MS_ion_mobility_spectrometry = 1000261,
    MS_IMS = MS_ion_mobility_spectrometry,
    MS_infrared_multiphoton_dissociation = 1000262,
    MS_IRMPD = MS_infrared_multiphoton_dissociation,
    MS_isotope_ratio_mass_spectrometry = 1000263,
    MS_IRMS = MS_isotope_ratio_mass_spectrometry,
    MS_ion_trap = 1000264,
    MS_IT = MS_ion_trap,
    MS_kinetic_energy_release_distribution = 1000265,
    MS_KERD = MS_kinetic_energy_release_distribution,
    MS_mass_analyzed_ion_kinetic_energy_spectrometry = 1000267,
    MS_MIKES = MS_mass_analyzed_ion_kinetic_energy_spectrometry,
    MS_mass_spectrometry = 1000268,
    MS_MS = MS_mass_spectrometry,
    MS_mass_spectrometry_mass_spectrometry = 1000269,
    MS_MS_MS = MS_mass_spectrometry_mass_spectrometry,
    MS_multiple_stage_mass_spectrometry = 1000270,
    MS_MSn = MS_multiple_stage_mass_spectrometry,
    MS_Negative_Ion_chemical_ionization = 1000271,
    MS_NICI = MS_Negative_Ion_chemical_ionization,
    MS_neutralization_reionization_mass_spectrometry = 1000272,
    MS_NRMS = MS_neutralization_reionization_mass_spectrometry,
    MS_photoionization = 1000273,
    MS_PI = MS_photoionization,
    MS_pyrolysis_mass_spectrometry = 1000274,
    MS_PyMS = MS_pyrolysis_mass_spectrometry,
    MS_collision_quadrupole = 1000275,
    MS_q = MS_collision_quadrupole,
    MS_resonance_enhanced_multiphoton_ionization = 1000276,
    MS_REMPI = MS_resonance_enhanced_multiphoton_ionization,
    MS_residual_gas_analyzer = 1000277,
    MS_RGA = MS_residual_gas_analyzer,
    MS_surface_enhanced_laser_desorption_ionization = 1000278,
    MS_SELDI = MS_surface_enhanced_laser_desorption_ionization,
    MS_surface_enhanced_neat_desorption = 1000279,
    MS_SEND = MS_surface_enhanced_neat_desorption,
    MS_selected_ion_flow_tube = 1000281,
    MS_SIFT = MS_selected_ion_flow_tube,
    MS_sustained_off_resonance_irradiation = 1000282,
    MS_SORI = MS_sustained_off_resonance_irradiation,
    MS_stored_waveform_inverse_fourier_transform = 1000284,
    MS_SWIFT = MS_stored_waveform_inverse_fourier_transform,
    MS_total_ion_current = 1000285,
    MS_TIC = MS_total_ion_current,
    MS_time_lag_focusing = 1000286,
    MS_TLF = MS_time_lag_focusing,
    MS_time_of_flight_mass_spectrometer = 1000287,
    MS_TOF_MS = MS_time_of_flight_mass_spectrometer,
    MS_cyclotron = 1000288,
    MS_double_focusing_mass_spectrometer = 1000289,
    MS_hybrid_mass_spectrometer = 1000290,
    MS_linear_ion_trap = 1000291,
    MS_mass_spectrograph_obsolete = 1000292,
    MS_mass_spectrometer = 1000293,
    MS_mass_spectrum = 1000294,
    MS_mattauch_herzog_geometry = 1000295,
    MS_nier_johnson_geometry = 1000296,
    MS_paul_ion_trap = 1000297,
    MS_prolate_traochoidal_mass_spectrometer = 1000298,
    MS_quistor = 1000299,
    MS_reflectron = 1000300,
    MS_sector_mass_spectrometer = 1000301,
    MS_tandem_mass_spectrometer = 1000302,
    MS_transmission_quadrupole_mass_spectrometer = 1000303,
    MS_accelerating_voltage = 1000304,
    MS_cyclotron_motion = 1000305,
    MS_dynamic_mass_spectrometry = 1000306,
    MS_einzel_lens = 1000307,
    MS_electric_field_strength = 1000308,
    MS_first_stability_region = 1000309,
    MS_fringing_field = 1000310,
    MS_kinetic_energy_analyzer = 1000311,
    MS_mass_limit = 1000312,
    MS_scan_m_z_range_ = 1000313,
    MS_mass_selective_axial_ejection = 1000314,
    MS_mass_selective_instability = 1000315,
    MS_mathieu_stability_diagram = 1000316,
    MS_orthogonal_extraction = 1000317,
    MS_resonance_ion_ejection = 1000318,
    MS_space_charge_effect = 1000319,
    MS_static_field = 1000320,
    MS_2E_Mass_Spectrum = 1000321,
    MS_charge_inversion_mass_spectrum = 1000322,
    MS_constant_neutral_loss_scan = 1000323,
    MS_constant_neutral_gain_scan = 1000324,
    MS_Constant_Neutral_Mass_Gain_Scan = MS_constant_neutral_gain_scan,
    MS_constant_neutral_mass_gain_spectrum = 1000325,
    MS_constant_neutral_mass_loss_spectrum = 1000326,
    MS_Constant_Neutral_Mass_Loss = MS_constant_neutral_mass_loss_spectrum,
    MS_e_2_mass_spectrum = 1000328,
    MS_linked_scan = 1000329,
    MS_linked_scan_at_constant_b_e = 1000330,
    MS_Linked_Scan_at_Constant_E2_V = 1000331,
    MS_Linked_Scan_at_Constant_B2_E = 1000332,
    MS_Linked_Scan_at_Constant_B_1__E_E0___1_2___E = 1000333,
    MS_MS_MS_in_Time = 1000334,
    MS_MS_MS_in_Space = 1000335,
    MS_neutral_loss = 1000336,
    MS_nth_generation_product_ion = 1000337,
    MS_nth_generation_product_ion_scan = 1000338,
    MS_nth_generation_product_ion_spectrum = 1000339,
    MS_precursor_ion = 1000340,
    MS_precursor_ion_spectrum = 1000341,
    MS_product_ion = 1000342,
    MS_product_ion_spectrum = 1000343,
    MS_progeny_ion = 1000344,
    MS_Progeny_Fragment_Ion = MS_progeny_ion,
    MS_array_detector = 1000345,
    MS_conversion_dynode = 1000346,
    MS_dynode = 1000347,
    MS_focal_plane_collector = 1000348,
    MS_ion_to_photon_detector = 1000349,
    MS_point_collector = 1000350,
    MS_postacceleration_detector = 1000351,
    MS_secondary_electron = 1000352,
    MS_adduct_ion = 1000353,
    MS_aromatic_ion = 1000354,
    MS_analog_ion = 1000355,
    MS_anti_aromatic_ion = 1000356,
    MS_cationized_molecule = 1000357,
    MS_cluster_ion = 1000358,
    MS_Conventional_ion = 1000359,
    MS_diagnostic_ion = 1000360,
    MS_dimeric_ion = 1000361,
    MS_distonic_ion = 1000362,
    MS_enium_ion = 1000363,
    MS_ion = 1000365,
    MS_Isotopologue_ion = 1000366,
    MS_Isotopomeric_ion = 1000367,
    MS_metastable_ion = 1000368,
    MS_molecular_ion = 1000369,
    MS_negative_ion = 1000370,
    MS_non_classical_ion = 1000371,
    MS_onium_ion = 1000372,
    MS_principal_ion = 1000373,
    MS_positive_ion = 1000374,
    MS_protonated_molecule = 1000375,
    MS_radical_ion = 1000376,
    MS_reference_ion = 1000377,
    MS_stable_ion = 1000378,
    MS_unstable_ion = 1000379,
    MS_adiabatic_ionization = 1000380,
    MS_associative_ionization = 1000381,
    MS_atmospheric_pressure_photoionization = 1000382,
    MS_autodetachment = 1000383,
    MS_autoionization = 1000384,
    MS_charge_exchange_ionization = 1000385,
    MS_chemi_ionization = 1000386,
    MS_desorption_ionization_on_silicon = 1000387,
    MS_dissociative_ionization = 1000388,
    MS_electron_ionization = 1000389,
    MS_ion_desolvation = 1000390,
    MS_ion_pair_formation = 1000391,
    MS_ionization_efficiency = 1000392,
    MS_laser_desorption_ionization = 1000393,
    MS_liquid_secondary_ionization = 1000395,
    MS_membrane_inlet = 1000396,
    MS_microelectrospray = 1000397,
    MS_nanoelectrospray = 1000398,
    MS_penning_ionization = 1000399,
    MS_plasma_desorption_ionization = 1000400,
    MS_pre_ionization_state = 1000401,
    MS_secondary_ionization = 1000402,
    MS_soft_ionization = 1000403,
    MS_spark_ionization = 1000404,
    MS_surface_assisted_laser_desorption_ionization = 1000405,
    MS_surface_ionization = 1000406,
    MS_thermal_ionization = 1000407,
    MS_vertical_ionization = 1000408,
    MS_association_reaction = 1000409,
    MS_alpha_cleavage = 1000410,
    MS_beta_cleavage = 1000411,
    MS_buffer_gas = 1000412,
    MS_charge_induced_fragmentation = 1000413,
    MS_charge_inversion_reaction = 1000414,
    MS_charge_permutation_reaction = 1000415,
    MS_charge_stripping_reaction = 1000416,
    MS_charge_transfer_reaction = 1000417,
    MS_collisional_excitation = 1000418,
    MS_collision_gas = 1000419,
    MS_heterolytic_cleavage = 1000420,
    MS_high_energy_collision = 1000421,
    MS_high_energy_collision_induced_dissociation = 1000422,
    MS_homolytic_cleavage = 1000423,
    MS_hydrogen_deuterium_exchange = 1000424,
    MS_ion_energy_loss_spectrum = 1000425,
    MS_ionizing_collision = 1000426,
    MS_ion_molecule_reaction = 1000427,
    MS_ion_neutral_complex = 1000428,
    MS_ion_neutral_species_reaction = 1000429,
    MS_ion_neutral_species_exchange_reaction = 1000430,
    MS_kinetic_method = 1000431,
    MS_low_energy_collisions = 1000432,
    MS_low_energy_collision_induced_dissociation = 1000433,
    MS_McLafferty_Rearrangement = 1000434,
    MS_photodissociation = 1000435,
    MS_Multiphoton_Dissociation = MS_photodissociation,
    MS_partial_charge_transfer_reaction = 1000436,
    MS_ion_reaction = 1000437,
    MS_superelastic_collision = 1000438,
    MS_surface_induced_reaction = 1000439,
    MS_unimolecular_dissociation = 1000440,
    MS_scan = 1000441,
    MS_spectrum = 1000442,
    MS_mass_analyzer_type = 1000443,
    MS_m_z_Separation_Method = 1000444,
    MS_sequential_m_z_separation_method__ = 1000445,
    MS_fast_ion_bombardment = 1000446,
    MS_FIB = MS_fast_ion_bombardment,
    MS_LTQ = 1000447,
    MS_LTQ_FT = 1000448,
    MS_LTQ_Orbitrap = 1000449,
    MS_LXQ = 1000450,
    MS_mass_analyzer = 1000451,
    MS_data_transformation = 1000452,
    MS_detector = 1000453,
    MS_instrument_additional_description = 1000454,
    MS_ion_selection_attribute = 1000455,
    MS_precursor_activation = 1000456,
    MS_sample = 1000457,
    MS_source = 1000458,
    MS_spectrum_instrument_description = 1000459,
    MS_unit = 1000460,
    MS_additional_description = 1000461,
    MS_ion_optics = 1000462,
    MS_instrument = 1000463,
    MS_mass_unit = 1000464,
    MS_scan_polarity = 1000465,
    MS_1200_series_LC_MSD_SL = 1000467,
    MS_6110_Quadrupole_LC_MS = 1000468,
    MS_6120_Quadrupole_LC_MS = 1000469,
    MS_6130_Quadrupole_LC_MS = 1000470,
    MS_6140_Quadrupole_LC_MS = 1000471,
    MS_6210_Time_of_Flight_LC_MS = 1000472,
    MS_6310_Ion_Trap_LC_MS = 1000473,
    MS_6320_Ion_Trap_LC_MS = 1000474,
    MS_6330_Ion_Trap_LC_MS = 1000475,
    MS_6340_Ion_Trap_LC_MS = 1000476,
    MS_6410_Triple_Quadrupole_LC_MS = 1000477,
    MS_6410_Triple_Quad_LC_MS = MS_6410_Triple_Quadrupole_LC_MS,
    MS_1200_series_LC_MSD_VL = 1000478,
    MS_purgatory = 1000479,
    MS_mass_analyzer_attribute = 1000480,
    MS_detector_attribute = 1000481,
    MS_source_attribute = 1000482,
    MS_Thermo_Fisher_Scientific_instrument_model = 1000483,
    MS_orbitrap = 1000484,
    MS_nanospray_inlet = 1000485,
    MS_source_potential = 1000486,
    MS_ion_optics_attribute = 1000487,
    MS_Hitachi_instrument_model = 1000488,
    MS_Varian_instrument_model = 1000489,
    MS_Agilent_instrument_model = 1000490,
    MS_Dionex_instrument_model = 1000491,
    MS_Thermo_Electron_instrument_model = 1000492,
    MS_Finnigan_MAT_instrument_model = 1000493,
    MS_Thermo_Scientific_instrument_model = 1000494,
    MS_Applied_Biosystems_instrument_model = 1000495,
    MS_ABI = MS_Applied_Biosystems_instrument_model,
    MS_instrument_attribute = 1000496,
    MS_zoom_scan = 1000497,
    MS_full_scan = 1000498,
    MS_spectrum_attribute = 1000499,
    MS_scan_m_z_upper_limit = 1000500,
    MS_scan_m_z_lower_limit = 1000501,
    MS_dwell_time = 1000502,
    MS_scan_attribute = 1000503,
    MS_base_peak_m_z = 1000504,
    MS_base_peak_intensity = 1000505,
    MS_ion_role = 1000506,
    MS_ion_attribute = 1000507,
    MS_ion_chemical_type = 1000508,
    MS_activation_energy = 1000509,
    MS_precursor_activation_attribute = 1000510,
    MS_ms_level = 1000511,
    MS_filter_string = 1000512,
    MS_binary_data_array = 1000513,
    MS_m_z_array = 1000514,
    MS_intensity_array = 1000515,
    MS_charge_array = 1000516,
    MS_signal_to_noise_array = 1000517,
    MS_binary_data_type = 1000518,
    MS_32_bit_integer = 1000519,
    MS_16_bit_float = 1000520,
    MS_32_bit_float = 1000521,
    MS_64_bit_integer = 1000522,
    MS_64_bit_float = 1000523,
    MS_data_file_content = 1000524,
    MS_spectrum_representation = 1000525,
    MS_MassLynx_raw_format = 1000526,
    MS_highest_m_z_value = 1000527,
    MS_lowest_m_z_value = 1000528,
    MS_instrument_serial_number = 1000529,
    MS_file_format_conversion = 1000530,
    MS_software = 1000531,
    MS_Xcalibur = 1000532,
    MS_Bioworks = 1000533,
    MS_Masslynx = 1000534,
    MS_FlexAnalysis = 1000535,
    MS_data_explorer = 1000536,
    MS_4700_Explorer = 1000537,
    MS_Wolf = 1000538,
    MS_Voyager_Biospectrometry_Workstation_System = 1000539,
    MS_FlexControl = 1000540,
    MS_ReAdW = 1000541,
    MS_MzStar = 1000542,
    MS_data_processing_action = 1000543,
    MS_Conversion_to_mzML = 1000544,
    MS_Conversion_to_mzXML = 1000545,
    MS_Conversion_to_mzData = 1000546,
    MS_object_attribute = 1000547,
    MS_sample_attribute = 1000548,
    MS_selection_window_attribute = 1000549,
    MS_time_unit = 1000550,
    MS_Analyst = 1000551,
    MS_maldi_spot_identifier = 1000552,
    MS_Trapper = 1000553,
    MS_LCQ_Deca = 1000554,
    MS_LTQ_Orbitrap_Discovery = 1000555,
    MS_LTQ_Orbitrap_XL = 1000556,
    MS_LTQ_FT_Ultra = 1000557,
    MS_GC_Quantum = 1000558,
    MS_spectrum_type = 1000559,
    MS_source_file_type = 1000560,
    MS_data_file_checksum_type = 1000561,
    MS_wiff_file = 1000562,
    MS_Xcalibur_RAW_file = 1000563,
    MS_mzData_file = 1000564,
    MS_pkl_file = 1000565,
    MS_mzXML_file = 1000566,
    MS_yep_file = 1000567,
    MS_MD5 = 1000568,
    MS_SHA_1 = 1000569,
    MS_spectra_combination = 1000570,
    MS_sum_of_spectra = 1000571,
    MS_binary_data_compression_type = 1000572,
    MS_median_of_spectra = 1000573,
    MS_zlib_compression = 1000574,
    MS_mean_of_spectra = 1000575,
    MS_no_compression = 1000576,
    MS_data_file = 1000577,
    MS_LCQ_Fleet = 1000578,
    MS_MS1_spectrum = 1000579,
    MS_Single_Stage_Mass_Spectrometry = MS_MS1_spectrum,
    MS_MSn_spectrum = 1000580,
    MS_multiple_stage_mass_spectrometry_spectrum = MS_MSn_spectrum,
    MS_CRM_spectrum = 1000581,
    MS_SIM_spectrum = 1000582,
    MS_SRM_spectrum = 1000583,
    MS_mzML_file = 1000584,
    MS_contact_person_attribute = 1000585,
    MS_contact_name = 1000586,
    MS_contact_address = 1000587,
    MS_contact_URL = 1000588,
    MS_contact_email = 1000589,
    MS_contact_organization = 1000590,
    MS_MzWiff = 1000591,
    MS_smoothing = 1000592,
    MS_baseline_reduction = 1000593,
    MS_low_intensity_data_point_removal = 1000594,
    MS_time_array = 1000595,
    MS_measurement_method = 1000596,
    MS_ion_optics_type = 1000597,
    MS_electron_transfer_dissociation = 1000598,
    MS_ETD = MS_electron_transfer_dissociation,
    MS_pulsed_q_dissociation = 1000599,
    MS_PQD = MS_pulsed_q_dissociation,
    MS_Proteios = 1000600,
    MS_ProteinLynx_Global_Server = 1000601,
    MS_Shimadzu_Biotech_instrument_model = 1000602,
    MS_Shimadzu_Scientific_Instruments_instrument_model = 1000603,
    MS_LCMS_IT_TOF = 1000604,
    MS_LCMS_2010EV = 1000605,
    MS_LCMS_2010A = 1000606,
    MS_AXIMA_CFR_MALDI_TOF = 1000607,
    MS_AXIMA_QIT = 1000608,
    MS_AXIMA_CFR_plus = 1000609,
    MS_AXIMA_Performance_MALDI_TOF_TOF = 1000610,
    MS_AXIMA_Confidence_MALDI_TOF = 1000611,
    MS_AXIMA_Assurance_Linear_MALDI_TOF = 1000612,
    MS_dta_file = 1000613,
    MS_ProteinLynx_Global_Server_mass_spectrum_XML_file = 1000614,
    MS_ProteoWizard = 1000615,
    MS_pwiz = MS_ProteoWizard,
    MS_preset_scan_configuration = 1000616
}; // enum CVID


/// structure for holding CV term info
struct CVInfo
{
    CVID cvid;
    std::string id;
    std::string name;
    std::string def;

    typedef std::vector<CVID> id_list;
    id_list parentsIsA;
    id_list parentsPartOf;
    std::vector<std::string> exactSynonyms;

    CVInfo() : cvid((CVID)-1) {}
    const std::string& shortName() const;
};


/// returns CV term info for the specified CVID
const CVInfo& cvinfo(CVID id);


/// returns true iff child IsA parent in the CV
bool cvIsA(CVID child, CVID parent);


/// returns vector of all valid CVIDs
const std::vector<CVID>& cvids();


} // namespace msdata
} // namespace pwiz


#endif // _CV_HPP_


