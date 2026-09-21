using System.Collections.Generic;
using Nox.XR.Bindings;

namespace Nox.XR.OpenXR {
	/// <summary>
	/// Chemins de bindings OpenXR : quel contrôle d'Input System alimente chaque binding logique.
	///
	/// <para>
	/// OpenXR est le runtime « propre » : les manettes y sont exposées par Unity sous le layout
	/// standard <c>XRController</c>, quel que soit le constructeur (Oculus, Index, Vive, WMR...).
	/// Un seul jeu de chemins suffit donc, contrairement à OpenVR où chaque modèle expose ses
	/// propres contrôles (voir les packs de nox.xr.openvr).
	/// </para>
	///
	/// <para>
	/// Les chemins sont exprimés en <b>usages</b> d'Input System (<c>{Trigger}</c>, <c>{Grip}</c>,
	/// <c>{Primary2DAxis}</c>, ...) : c'est le vocabulaire commun que le plugin OpenXR de Unity
	/// renseigne, il ne dépend donc pas du nom des contrôles du device.
	/// </para>
	///
	/// <para>
	/// Utilisé par <see cref="OpenXRBindings"/>, qui enregistre ces chemins et répond aux lectures.
	/// </para>
	/// </summary>
	internal static class OpenXRBindingProvider {
		/// <summary>
		/// Contrôles par binding logique, par ordre de préférence.
		///
		/// <para>
		/// Le premier contrôle qui existe réellement sur une manette connectée est retenu
		/// (voir <see cref="XRBindingPaths.Resolve"/>) ; les suivants servent de repli quand un
		/// runtime/une manette n'expose pas l'usage attendu.
		/// </para>
		/// </summary>
		private static readonly Dictionary<XRBinding, string[]> Controls = new() {
			{ XRBinding.MenuLeft,  new[] { "{SecondaryButton}", "{MenuButton}" } },
			{ XRBinding.MenuRight, new[] { "{SecondaryButton}", "{MenuButton}" } },
			{ XRBinding.Jump,      new[] { "{PrimaryButton}" } },

			{ XRBinding.SelectLeft,     new[] { "{Grip}" } },
			{ XRBinding.SelectRight,    new[] { "{Grip}" } },
			{ XRBinding.ActivateLeft,   new[] { "{Trigger}" } },
			{ XRBinding.ActivateRight,  new[] { "{Trigger}" } },
			{ XRBinding.PressLeft,      new[] { "{Trigger}" } },
			{ XRBinding.PressRight,     new[] { "{Trigger}" } },

			{ XRBinding.FingerLeftThumb,  new[] { "{PrimaryTouch}", "{Thumbrest}" } },
			{ XRBinding.FingerLeftIndex,  new[] { "{Trigger}" } },
			{ XRBinding.FingerLeftMiddle, new[] { "{Grip}" } },
			{ XRBinding.FingerLeftRing,   new[] { "{Grip}" } },
			{ XRBinding.FingerLeftPinky,  new[] { "{Grip}" } },

			{ XRBinding.FingerRightThumb,  new[] { "{PrimaryTouch}", "{Thumbrest}" } },
			{ XRBinding.FingerRightIndex,  new[] { "{Trigger}" } },
			{ XRBinding.FingerRightMiddle, new[] { "{Grip}" } },
			{ XRBinding.FingerRightRing,   new[] { "{Grip}" } },
			{ XRBinding.FingerRightPinky,  new[] { "{Grip}" } },

			// Unity expose les axes sous leur nom (Primary2DAxis) et pas toujours en usage.
			{ XRBinding.Move,        new[] { "Primary2DAxis", "{Primary2DAxis}" } },
			{ XRBinding.Turn,        new[] { "Primary2DAxis", "{Primary2DAxis}" } },
			{ XRBinding.ScrollLeft,  new[] { "Primary2DAxis", "{Primary2DAxis}" } },
			{ XRBinding.ScrollRight, new[] { "Primary2DAxis", "{Primary2DAxis}" } },
		};

		/// <summary>
		/// Bindings OpenXR à lier, avec le chemin retenu pour la main visée.
		///
		/// <para>
		/// Le repli sur le premier candidat quand aucun device n'est connecté est volontaire : le
		/// chemin se liera dès qu'une manette apparaîtra, et nox.xr relance la liaison à ce
		/// moment-là.
		/// </para>
		/// </summary>
		public static IEnumerable<(XRBinding Binding, string Path)> GetBindings() {
			foreach (var target in Controls) {
				var hand       = target.Key.GetHand();
				var candidates = new List<string>(target.Value.Length);
				foreach (var control in target.Value)
					candidates.Add(XRBindingPaths.Build(hand, null, control));

				var path = XRBindingPaths.Resolve(candidates);
				if (string.IsNullOrWhiteSpace(path))
					continue;

				yield return (target.Key, path);
			}
		}
	}
}
