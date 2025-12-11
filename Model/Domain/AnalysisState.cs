using System;

namespace GbService.Model.Domain
{
	public enum AnalysisState
	{
		EnCours,
		EnvoyerAutomate,
		ReçuAutomate,
		ValidéTechnique,
		Invalide,
		ValidéBiologique,
		EnvoyéSousTraitance,
		D1,
		D2,
		NonConforme,
		ÀEnvoyer,
		Archivé
	}
}
