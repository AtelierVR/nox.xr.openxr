using System;
using System.Collections.Generic;
using Nox.CCK.Mods.Cores;
using Nox.KeyBindings;
using Nox.XR.Bindings;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.XR.OpenXR {
	/// <summary>
	/// Implémentation <see cref="IBinding"/> d'OpenXR.
	///
	/// <para>
	/// Enregistre auprès du système de key bindings les chemins délivrés par
	/// <see cref="OpenXRBindingProvider"/> et répond aux lectures en interrogeant directement les
	/// actions : c'est le mod qui possède ses bindings, nox.xr ne fait que déclencher
	/// <see cref="Refresh"/> et relayer les valeurs.
	/// </para>
	/// </summary>
	public sealed class OpenXRBindings : IBinding {
		private readonly IMainModCoreAPI _api;

		/// <summary>Poignées des actions enregistrées, par clé de binding.</summary>
		private readonly Dictionary<string, IKeyBinding> _handles = new();

		/// <summary>Binding logique de chaque clé : donne le type de valeur attendu.</summary>
		private readonly Dictionary<string, XRBinding> _bindings = new();

		public OpenXRBindings(IMainModCoreAPI api)
			=> _api = api;

		private IKeyBindingManager Manager
			=> _api?.ModAPI?.GetMod("keybinding")?.GetInstance<IKeyBindingManager>();

		public void Refresh() {
			Clear();

			var manager = Manager;
			if (manager == null) {
				Logger.LogWarning("OpenXR: key binding manager not available, XR inputs are left unbound.");
				return;
			}

			foreach (var (binding, path) in OpenXRBindingProvider.GetBindings()) {
				var key    = binding.GetKey();
				var handle = manager.AddKeyBinding(key, path, binding.GetCategory());
				if (handle == null) {
					Logger.LogError($"OpenXR: failed to register key binding {key} ({path})");
					continue;
				}

				// Lecture directe de l'action : aucun écouteur ici, nox.xr lit à la demande.
				handle.GetAction()?.Enable();
				_handles[key]  = handle;
				_bindings[key] = binding;
			}

			Logger.LogDebug($"OpenXR: {_handles.Count} key binding(s) registered.");
		}

		public void Clear() {
			var manager = Manager;
			if (manager != null)
				foreach (var handle in _handles.Values)
					manager.RemoveKeyBinding(handle.GetId(), handle.GetCategory());

			_handles.Clear();
			_bindings.Clear();
		}

		public T Get<T>(string key) where T : struct {
			if (!_bindings.TryGetValue(key, out var binding))
				return default;

			// Évite que ReadValue<T> lève quand le type demandé ne correspond pas au binding.
			var isVector2 = binding.GetValue() == XRBindingValue.Vector2;
			if (isVector2 != (typeof(T) == typeof(Vector2)))
				return default;

			var action = _handles.TryGetValue(key, out var handle) ? handle.GetAction() : null;
			return action is { enabled: true } ? action.ReadValue<T>() : default;
		}
	}
}
