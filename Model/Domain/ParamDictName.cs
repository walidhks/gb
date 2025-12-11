using System;
using System.ComponentModel.DataAnnotations;

namespace GbService.Model.Domain
{
	public enum ParamDictName
	{
		[Display(Name = "Nom d'établissement")]
		EtabName,
		[Display(Name = "L'adresse d'établissement")]
		EtabAddress,
		[Display(Name = "Email d'établissement")]
		EtabEmail,
		[Display(Name = "sous-nom d'établissement")]
		EtabSubName,
		[Display(Name = "Tél d'établissement")]
		EtabTel,
		[Display(Name = "Agrément d'établissement")]
		LaboAgrement,
		[Display(Name = "Serveur de mise à jour")]
		UpdateServerPath,
		[Display(Name = "Identificateur d'établissement")]
		EtabId,
		[Display(Name = "Email de publication")]
		EmailDePublication,
		[Display(Name = "Client SMTP")]
		SmtpClient,
		[Display(Name = "Mot de passe Email de publication")]
		ModeDePasseEmailDePublication,
		[Display(Name = "sous-nom d'établissement 2")]
		EtabSubName2,
		[Display(Name = "Identification Fiscal")]
		IdenFiscal,
		[Display(Name = "N ART Impo")]
		NArtImpo,
		[Display(Name = "Compte Banquaire")]
		CompteBanque,
		[Display(Name = "Image du cachet")]
		CachetImage,
		[Display(Name = "Impression du ticket de reçu")]
		ImpressionTicketReçu,
		[Display(Name = "Impression jeton de prélèvement")]
		ImpressionJetonPrelev,
		[Display(Name = "Serveur de Planification")]
		ScheduleServer,
		[Display(Name = "Chaine de connexion bdd du serveur de planification")]
		ConnectionStringScheduleServerDb,
		[Display(Name = "SMS Port")]
		SmsPort,
		[Display(Name = "SMS Date Début")]
		SmsStart,
		[Display(Name = "TVA")]
		Tva,
		[Display(Name = "SMS Notification des résultats : en Arabe")]
		Sms1ArabicContent,
		[Display(Name = "SMS Notification des résultats : en Français")]
		Sms1FrenchContent,
		[Display(Name = "SMS Notification pour reprélèvement : en Arabe")]
		Sms2ArabicContent,
		[Display(Name = "SMS Notification pour reprélèvement : en Français")]
		Sms2FrenchContent,
		[Display(Name = "Titre de l'email")]
		EmailSubject,
		[Display(Name = "Contenu de l'email")]
		EmailContent,
		[Display(Name = "Chemin de stockage des pièces jointes")]
		AttachementFileStorePath,
		[Display(Name = "Début de l'interval des numéro dossier")]
		AnalysisRequestStartInterval,
		[Display(Name = "Ordonnance Obligatoire")]
		OrdonnanceRequired,
		[Display(Name = "Chemin de stockage des demandes")]
		DemandeFileStorePath,
		[Display(Name = "Nombre de position code à bare")]
		NumberPositionBarcode,
		[Display(Name = "Validation Biologique Obligatoire avant l'impression")]
		BioValidationRequired,
		[Display(Name = "Cachet et Signature dans les feuilles de résultats")]
		CachetEtSignatureOfResultReport,
		[Display(Name = "Montant KB du prélèvement")]
		SampleKbAmount,
		[Display(Name = "Tiket code bare largeur")]
		BarecodeLabelWidth,
		[Display(Name = "Tiket code bare hauteur")]
		BarecodeLabelHeight,
		[Display(Name = "Azure Store Connection String")]
		AzureStoreConnectionString,
		[Display(Name = "Lien du serveur Web")]
		ResultWebServerLink,
		[Display(Name = "Séparation des tubes par unité")]
		SampleTubeSeparationByUnit,
		[Display(Name = "Image Logo")]
		ImageLogoEtab,
		[Display(Name = "Ouverture automatique de la caisse")]
		CashDrawer,
		[Display(Name = "Numéro Tel. Destinataire pour SMS de validation")]
		DestiantionSmsAskValidation,
		[Display(Name = "Type de code bare des tubes")]
		SampleBareCodeSymbology,
		[Display(Name = "Code de l'analyse du Groupage Abo Rh")]
		AboRhAnalysisTypeId,
		[Display(Name = "Code de l'analyse du Groupage Phenotype")]
		GroupPhenotypeAnalysisTypeId,
		[Display(Name = "Méthode de calcule du cotation B")]
		CotationBCalculationMethod,
		[Display(Name = "Numéro du dossier courant")]
		AnalysisRequestCurrentId2,
		[Display(Name = "Double saisie du groupage")]
		GroupageDoubleEntry,
		[Display(Name = "Commentaire par défaut")]
		CashDrawer2,
		[Display(Name = "Soutraitance Container")]
		OutsourcingAzureStorageContainer,
		[Display(Name = "Rapports Container")]
		AnalysisRequestAzureStorageContainer,
		[Display(Name = "Intervalle de recherche (Jours)")]
		SearchInterval,
		[Display(Name = "Identifiant publique du Laboratoire")]
		PublicLaboratoryId,
		[Display(Name = "L'intervalle de la remise à zéro des numéros dossiers")]
		AnalysisRequestIdResetInterval,
		[Display(Name = "Validation Technique Obligatoire")]
		TechValidationRequired,
		[Display(Name = "Entete Rapport")]
		HeaderReportRichText,
		[Display(Name = "Code de l'analyse FNS")]
		FnsId,
		[Display(Name = "Format d'impression des enveloppe")]
		EnvelopePrintingFormat,
		[Display(Name = "Entête des autres page du Rapport")]
		HeaderOtherReportPageRichText,
		[Display(Name = "Image du cachet des cartes de groupage")]
		CachetAboImage,
		[Display(Name = "Longueur du compteur des demandes")]
		CounterLenght
	}
}
