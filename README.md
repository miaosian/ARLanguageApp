AR Language Learning App
Developed by: KEE MIAO SIAN
Final Year Project – MMU
Platform: iOS (Unity + AR Foundation + Firebase + Google APIs)

Prerequisites
Unity Hub (Version 3 or newer)

Unity Editor (same version used in development)

Xcode (for iOS build)

iPhone/iPad with ARKit support

How to Run
Clone the repo
git clone https://github.com/miaosian/ARLanguageApp.git

Or download the code in github

Open in Unity

Open the project folder with Unity Hub

Let Unity load and install required packages

Switch Build Target to iOS

File > Build Settings > iOS > Switch Platform

Open Xcode

Click “Build” in Unity  -> and choose the folder name "AR" to build

After Build success -> rightclick the "AR" Folder -> open the Terminal -> enter "pod install"

Opens Xcode project

Set developer account

Project navigator -> Unity Iphone -> Signing & Capabilities

under Signing -> Team -> select your developer account

Build & run on connected iPhone (physical device only)


Folder Structure
Assets/Scripts/ – Core scripts like ChallengeManager, ScanManager, etc.

Assets/Prefabs/ – 3D object prefabs for vocabulary

Assets/Scenes/ – Login, Progress, Challenge, Contextual scenes

Disclaimer
This is a final year academic project and not intended for commercial distribution.
