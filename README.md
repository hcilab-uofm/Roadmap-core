# Roadmap

This is a unity package that delivers the authoring application for the UBCO roadmap. It has two components, a VR application and an AR application. Functionally they are the same, they allow to place, manipulate and persist models in the environment. 

- **AR Application**: The AR application is built for mobile AR (Android/iOS). It uses the google's geospacial API, which uses the google streetview to localize and persist model locations between sessions with much better accuracy. See more information in the API: https://developers.google.com/ar/develop/geospatial

- **VR Application**: This is an Oculus application that allows the users to build out the environments in the VR when they are iterating on the designs. The current version mimcs the environment along the pathway north of the UBCO Campus.

## Installaition
Note that the guides are written primarily for android, but they apply to iOS builds as well
### Install prerequisites
- To ease things, make sure the project is switched to android platform (in build settings), the color space is switched to `linear` (in `project settings` > `player`).
- Install and configure [Oculus Integration for unity](https://developer.oculus.com/documentation/unity/unity-gs-overview/).
- Install and configure [MRTK](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/?view=mrtkunity-2022-05) for [Oculus](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/supported-devices/oculus-quest-mrtk?view=mrtkunity-2022-05) and [Mobile AR](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/supported-devices/using-ar-foundation?view=mrtkunity-2022-05)
- Install [AR Core extensions](https://github.com/google-ar/arcore-unity-extensions.git)
  - In the pacakge manager, select "Add Package from git URL" from the dropdown on the topleft corner (the `+` sign).
  - Enter the url `https://github.com/google-ar/arcore-unity-extensions.git`
- Ensure the ARCore, ARKit and Oculus XR plugins are installed.
  - In the `project settings`, select and `XR plug-in management`.
  - Install if it's not already installed.
  - Under the android tab, select `ARCore` and `Oculus`
  - Under iOS tab, select `ARKit`
### Install and setup roadmap
- Install package from git:
  - In the pacakge manager, select "Add Package from git URL" from the dropdown on the topleft corner (the `+` sign).
  - Enter the url `https://github.com/hcilab-uofm/Roadmap-core.git`
- Once it finishes installing, setup URP. Optionally, you may use the profiles shipped with this package. In the project windows it can be found in `Pacakges` > `roadmap-core` > `Assets` > `Essentials` > `Settings`
  - In `Project Settings` > `Graphics`, add a `Universal Render Pipeline Asset` under "Scriptable render pipeline assets" (`UniversalRenderPipelineAsset_StandardQuality`)
  - In `Project Settings` > `Graphics` > `URP Global Settings`, add  a `Universal Render Pipeline Global Settings` asset (`UniversalRenderPipelineGlobalSettings`)
  - In  `Project Settings` > `Quality`, add a `Universal Render Pipeline Asset` under "Render pipeline asset"  (`UniversalRenderPipelineAsset_StandardQuality`)
  - Update the MRTK shaders: `Mixed Reality` > `Toolkit` > `Utilities` > `Upgrade MRTK Standard Shaders for Universal Render Pipeline`
- Install [patch](Media/patch.diff). This is used to override the Oculus plugin changing the screen orientation.
  - From the root folder of your project:
  ```sh
  git apply <path to patch.diff>
  ```
- Follow instructions in [this issue comment](https://github.com/microsoft/MixedRealityToolkit-Unity/issues/10449#issuecomment-1111163353) if you get the following error when building:
```sh
Shader error in 'Mixed Reality Toolkit/Standard'
```


## Usage
- WIP

## Acknowledgement
- The AR version of the projects is built from the amazing PocketGarden implementation: https://github.com/buck-co/PocketGarden
