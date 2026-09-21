using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.CCK.XR;
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
		/// <summary>
		/// nox.xr s'initialise avant ses mods de loader : c'est ici qu'on lui signale le nôtre.
		/// </summary>
		public void OnInitializeMain(IMainModCoreAPI api)
			=> XRLoaderEditorRegistry.Register(this);

		public void OnDisposeMain()
			=> XRLoaderEditorRegistry.Unregister(this);

		/// <summary>Priorité du loader OpenXR (cf. <c>XRManagementLoaderProvider.DefaultPriority</c> = 0).</summary>
		public const int DefaultPriority = 20;

		public string Id
			=> "openxr";

		public int Priority
			=> DefaultPriority;

		public bool IsSupported(Platform platform)
			=> IsPlatformSupported(platform);

		public XRLoader Loader
			=> XRLoaderAssets.Find<OpenXRLoader>();

		public bool IsValid
			=> IsPlatformSupported(PlatformExtensions.CurrentPlatform)
				// Sans loader OpenXR configuré, XR Plug-in Management n'a rien à démarrer :
				// ce provider s'efface (`StartAsync<OpenXRLoader>()` échouerait de toute façon).
				&& XRManagementLoader.HasLoader<OpenXRLoader>();

		/// <summary>
		/// Plateformes pour lesquelles Unity fournit un loader OpenXR.
		/// Linux et macOS en sont exclus (pas de plugin OpenXR côté Unity).
		/// </summary>
		public static bool IsPlatformSupported(Platform platform)
			=> platform switch {
				Platform.Windows => true,
				Platform.Android => true,
				Platform.VisionOS => true,
				_ => false,
			};

		public UniTask<bool> Initialize()
			=> XRManagementLoader.StartAsync<OpenXRLoader>();

		public UniTask Deinitialize()
			=> XRManagementLoader.Stop();
	}
}
