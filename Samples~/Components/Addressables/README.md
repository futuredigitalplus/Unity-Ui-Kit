# Addressables sample (Components module)

This sample needs the Addressables package (`com.unity.addressables` 1.18.19 or newer). Its assemblies compile
only when that package is installed (see `UICOMPONENTS_ADDRESSABLES_INSTALLED` in the asmdefs).

Before the example window works you have to:

1. Import the sample (Package Manager > Future Digital Plus UI Kit > Samples).
2. Mark the assets in the sample's `Data/` folder (`AddressablesExampleComponent.uxml`, `.uss` and `Box.uss`)
   as Addressable. The address of each asset must be its full asset path (do not use "Simplify Addressable
   Names").
3. Change the `[AssetRoot("Assets/Samples/Addressables/Data/")]` attribute in `AddressablesExampleComponent.cs`
   to the folder the sample was imported to (Unity imports samples to
   `Assets/Samples/<package display name>/<version>/<sample name>/`).

Open the example from the menu `Tools > Future Digital Plus UI Kit > Components Examples > Loading Assets > Addressables`.
