using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR;

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
		/// <summary>
		/// Ids des features OpenXR à activer pour Standalone (celles qu'Unity ne coche pas par
		/// défaut). On les cible par leur id pour ne pas dépendre de l'assembly qui les déclare.
		/// </summary>
		private static readonly string[] FeatureIds = {
			// HTC Vive Focus 3 (com.htc.upm.vive.openxr).
			"vive.openxr.feature.focus3controller",
			// VIVE Cosmos Controller (com.htc.upm.vive.openxr).
			"vive.openxr.feature.cosmoscontroller",
			// HTC VIVE Tracker Profile (com.htc.upm.vive.openxr).
			"com.massive.openxr.feature.input.htcvivetracker"
		};

		public void OnInitializeEditor(IEditorModCoreAPI api) 
			=> EnableFeatures();


		public void OnDisposeEditor() { }


		/// <summary>
		/// Active les features listées dans <see cref="FeatureIds"/> pour Standalone. Unity ne livre
		/// pas le profil des manettes HTC Vive Focus 3 : sans lui les manettes bougent (pose) mais
		/// aucun bouton/gâchette n'est lié.
		/// </summary>
		private static void EnableFeatures() {
			const BuildTargetGroup group = BuildTargetGroup.Standalone;

			var features = FeatureIds
				.Select(id => FeatureHelpers.GetFeatureWithIdForBuildTarget(group, id))
				.ToArray();

			// Le plugin vient peut-être d'être ajouté : l'asset de settings ne connaît pas encore
			// les features, on le rafraîchit alors une fois.
			if (features.Any(f => f == null)) {
				FeatureHelpers.RefreshFeatures(group);
				features = FeatureIds
					.Select(id => FeatureHelpers.GetFeatureWithIdForBuildTarget(group, id))
					.ToArray();
			}

			var changed = false;
			foreach (var feature in features) {
				if (feature == null || feature.enabled)
					continue;

				feature.enabled = true;
				EditorUtility.SetDirty(feature);
				changed = true;
			}

			if (!changed)
				return;

			var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
			if (settings)
				EditorUtility.SetDirty(settings);

			AssetDatabase.SaveAssets();
		}
	}
}
