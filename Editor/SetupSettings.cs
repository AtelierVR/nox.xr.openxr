using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEditor;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace Nox.XR.OpenXR.Editor {
	/// <summary>
	/// Réglages OpenXR appliqués à l'ouverture de l'éditeur.
	///
	/// <para>
	/// Les réglages communs (démarrage automatique, liste des loaders selon l'OS) sont gérés par
	/// <c>XRSettingsSetup</c> de nox.xr : ici il ne reste que le spécifique OpenXR.
	/// </para>
	/// </summary>
	public class SetupSettings : IEditorModInitializer {
		public void OnInitializeEditor(IEditorModCoreAPI api) {
			EnableViveTrackerProfile();
		}

		public void OnDisposeEditor() { }

		/// <summary>
		/// Active le profil d'interaction HTC Vive Tracker pour Standalone, afin que les
		/// Vive Trackers soient détectés sans le plugin SteamVR.
		/// </summary>
		private static void EnableViveTrackerProfile() {
			var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
			if (settings == null)
				return;

			var feature = settings.GetFeature<HTCViveTrackerProfile>();
			if (feature?.enabled != false)
				return;

			feature.enabled = true;
			EditorUtility.SetDirty(settings);
			AssetDatabase.SaveAssets();
		}
	}
}
