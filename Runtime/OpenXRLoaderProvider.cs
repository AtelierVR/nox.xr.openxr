using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.CCK.XR;
using Nox.XR.Bindings;
using Nox.XR.Loaders;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace Nox.XR.OpenXR {
	/// <summary>
	/// Loader OpenXR pour nox.xr.
	///
	/// <para>
	/// Volontairement plus prioritaire que <c>nox.xr.openvr</c> : sur Windows c'est OpenXR qui
	/// est utilisé, avec OpenVR en repli. Sur Linux, Unity ne fournit pas de loader OpenXR :
	/// <see cref="IsValid"/> renvoie faux et nox.xr bascule sur OpenVR.
	/// </para>
	///
	/// <para>
	/// nox.xr ne démarre jamais « le premier loader qui répond » : <see cref="Initialize"/> impose
	/// <see cref="OpenXRLoader"/>, donc ce provider ne peut pas se retrouver à piloter OpenVR.
	/// </para>
	///
	/// <para>
	/// Implémente <see cref="IMainModInitializer"/> parce que le loader de mods n'instancie que
	/// les entrypoints qui implémentent <c>IModInitializer</c> : c'est cette instance que
	/// <c>IMod.GetInstances&lt;IXRLoaderProvider&gt;()</c> retrouve ensuite.
	/// </para>
	/// </summary>
	public sealed class OpenXRLoaderProvider : IXRLoaderEditorProvider, IMainModInitializer {
		/// <summary>Bindings OpenXR, exposés à nox.xr tant que le loader est initialisé.</summary>
		private OpenXRBindings _binding;

		/// <summary>API principale, livrée par <see cref="OnInitializeMain"/> (qui peut arriver après <see cref="Initialize"/>).</summary>
		private IMainModCoreAPI _api;

		/// <summary><c>true</c> une fois le loader OpenXR démarré par <see cref="Initialize"/>.</summary>
		private bool _started;

		/// <summary>
		/// nox.xr s'initialise <b>avant</b> ses mods de loader : son initialiseur client peut donc
		/// démarrer la XR via <see cref="Initialize"/> avant que cet initialiseur principal ne
		/// construise les bindings. On retient l'API et, si la XR tourne déjà, on enregistre les
		/// bindings tout de suite (<see cref="Initialize"/> ne pouvait pas le faire).
		/// </summary>
		public void OnInitializeMain(IMainModCoreAPI api) {
			_api = api;
			XRLoaderEditorRegistry.Register(this);

			EnsureBindings();
			if (_started)
				_binding.Initialize();
		}

		public void OnDisposeMain() {
			_binding?.Dispose();
			_binding = null;
			_api     = null;
			XRLoaderEditorRegistry.Unregister(this);
		}

		/// <summary>
		/// Crée les bindings dès que l'API est connue. Tant qu'elle ne l'est pas (initialiseur
		/// principal pas encore appelé), <see cref="Binding"/> reste <c>null</c> : nox.xr lit alors
		/// des entrées non liées, jusqu'à l'enregistrement par <see cref="OnInitializeMain"/>.
		/// </summary>
		private void EnsureBindings() {
			if (_binding == null && _api != null)
				_binding = new OpenXRBindings(_api);
		}

		/// <summary>
		/// Bindings du runtime OpenXR : c'est le loader qui les enregistre auprès de nox.keybinding
		/// à l'initialisation (<see cref="Initialize"/>) et les retire à l'arrêt
		/// (<see cref="Deinitialize"/>), et qui répond aux lectures. nox.xr ne fait que relayer les
		/// valeurs.
		/// </summary>
		public IBinding Binding
			=> _binding;

		/// <summary>Priorité du loader OpenXR (cf. <c>XRManagementLoaderProvider.DefaultPriority</c> = 0).</summary>
		public const int DefaultPriority = 20;

		/// <summary>
		/// Identifiant du loader. C'est aussi celui que nox.xr cherche côté bindings
		/// (<see cref="OpenXRBindingProvider.Id"/>), pour associer le provider au loader actif.
		/// </summary>
		public const string DefaultId = "openxr";

		public string Id
			=> DefaultId;

		public int Priority
			=> DefaultPriority;

		public bool IsSupported(Platform platform)
			=> IsPlatformSupported(platform);

		public XRLoader Loader
			=> XRLoaderAssets.Find<OpenXRLoader>();

		/// <summary>
		/// Indique si OpenXR est réellement utilisable ici : plateforme supportée <b>et</b> loader
		/// déclaré dans XR Plug-in Management. Sert au loader comme au provider de bindings.
		/// </summary>
		public static bool IsAvailable
			=> IsPlatformSupported(PlatformExtensions.CurrentPlatform)
				// Sans loader OpenXR configuré, XR Plug-in Management n'a rien à démarrer :
				// ce provider s'efface (`StartAsync<OpenXRLoader>()` échouerait de toute façon).
				&& XRManagementLoader.HasLoader<OpenXRLoader>();

		public bool IsValid
			=> IsAvailable;

		/// <summary>
		/// Plateformes pour lesquelles Unity fournit un loader OpenXR.
		/// Linux et macOS en sont exclus (pas de plugin OpenXR côté Unity).
		/// </summary>
		public static bool IsPlatformSupported(Platform platform)
			=> platform == Platform.Windows 
				|| platform == Platform.Linux 
				|| platform == Platform.Android 
				|| platform == Platform.VisionOS;

		/// <summary>
		/// Démarre OpenXR, puis enregistre nos bindings auprès de nox.keybinding.
		///
		/// <para>
		/// L'enregistrement se fait ici et non à la construction du provider : les chemins sont
		/// résolus contre les manettes réellement connectées (<c>XRBindingPaths.Resolve</c>), et
		/// elles n'existent qu'une fois le runtime démarré. Si OpenXR ne démarre pas, rien n'est
		/// enregistré — un binding qu'on ne peut pas lire n'a rien à faire dans la liste.
		/// </para>
		/// </summary>
		public async UniTask<bool> Initialize() {
			if (!await XRManagementLoader.StartAsync<OpenXRLoader>())
				return false;

			_started = true;

			EnsureBindings();
			if (_binding != null)
				_binding.Initialize();
			else
				Logger.LogWarning(
					"OpenXR: key bindings are created by the main initializer which has not run yet; "
					+ "they will be registered as soon as it does."
				);

			return true;
		}

		/// <summary>
		/// Arrête OpenXR, puis retire nos bindings : ils pointent sur des actions liées aux devices
		/// que XR Plug-in Management vient de détruire.
		/// </summary>
		public async UniTask Deinitialize() {
			_started = false;
			await XRManagementLoader.Stop();
			_binding?.Deinitialize();
		}
	}
}
