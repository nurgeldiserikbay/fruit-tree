Published source: https://docs.google.com/document/d/1Nzpt4AQguh7-qSnbeaTSeHP92ibIUDCAlaDE_JB_tAo/edit (keep this file and the Google Doc identical).

# Fruit Tree Privacy Policy
Effective date and last updated: 26 November 2026
App: Fruit Tree (Android, package com.thelightcome.fruittree)  
Developer: TheLightCome  
Contact: nurikserikbay1995@gmail.com

## Summary

Fruit Tree is a calm, offline 3D game about picking fruit from trees. It is made for players of all ages, including children. The game has no accounts, no chat and no sign-in, and it does not ask for your name, contacts, photos or location. Your game progress stays on your device. The game shows ads from Google AdMob that are set up for children: they are not personalised and are limited to family-friendly content. You can remove ads with a one-time purchase through Google Play.

## Information stored on your device

The game saves the following on your device only:

- which trees you have unlocked and which fruit you have picked;
- your best results and round progress;
- settings such as sound and music volume, graphics quality and reduced motion;
- how many tree changes have passed since the last ad, so ads are not shown too often;
- whether you have bought Remove Ads.

The developer does not run servers for this game and does not receive a copy of this information. It is deleted when you uninstall the game or clear its data in Android settings. Android may include it in your own device backup, depending on your settings.

## Advertising (Google AdMob)

The free version shows a full-screen ad from time to time, at most once every three tree changes. Ads are provided by Google AdMob, a service of Google LLC.

Every ad request from Fruit Tree is marked as directed to children and to users under the age of consent, and is limited to content rated G (suitable for all audiences). As a result:

- ads are not personalised and are not based on your interests or activity in other apps;
- the game does not use the Android Advertising ID (the AD_ID permission is removed from the app);
- no ad is shown when the game starts;
- AdMob still processes limited information needed to deliver and measure ads and to prevent fraud. According to Google's disclosure for the Google Mobile Ads SDK, this includes your IP address (which may be used to estimate a general location), interactions such as ad taps and video views, diagnostic information such as app launch time, and an app-specific identifier (App Set ID). This information is encrypted in transit.

Google processes this information under its own policies: https://policies.google.com/privacy and https://policies.google.com/technologies/partner-sites

## Purchases (Google Play)

The game offers one optional, one-time purchase, "Remove Ads". Payment is handled entirely by Google Play. The developer never sees or stores your card or payment details. The game receives only a confirmation from Google Play that the purchase was made, so it can turn off ads and restore the purchase on a new device. Purchases are processed through Google Play Billing and the Unity In-App Purchasing library. Google's privacy policy applies to the payment: https://policies.google.com/privacy

The purchase button is placed behind a grown-up check (press and hold) so that young children do not open it by accident.

## Game engine

Fruit Tree is built with the Unity engine. Unity may collect limited technical information about the device and app performance as described in Unity's privacy policy: https://unity.com/legal/privacy-policy

## Children

Fruit Tree is suitable for children and follows the Google Play Families Policy. We treat every player as possibly a child (under 13, or under the age of digital consent in your country, such as 16 in parts of the European Union). For that reason the game never asks for an account, a name or any other personal details, and it has no chat, social features or links that let children share personal information. We do not knowingly collect personal information from children. The third-party services that receive data are Google AdMob, configured for child-directed treatment as described above, Google Play for the optional purchase, and Unity for limited technical information.

Parents and guardians can ask us at any time to review or delete information about their child, or to stop further collection, by writing to the email below. We do not keep such information on our own servers, so we will explain what is stored on the device and help you contact Google where needed. Uninstalling the game removes all of its local data.

## Sharing and selling

We do not sell personal information. We do not share information with anyone except Google (AdMob and Google Play) and Unity, as described above, to make the game, its ads and its purchase work.

## Legal basis (EEA and UK)

Where the GDPR or UK GDPR applies, the limited data described in this policy is processed on these grounds: our legitimate interest in showing non-personalised, family-friendly ads that keep the game free; performance of a contract when you buy Remove Ads; and our legitimate interest in keeping the game working and secure.

## International transfers

Google and Unity may process data in countries other than your own, including the United States. They state that they protect such transfers with recognised safeguards, such as the European Commission's Standard Contractual Clauses.

## Security and retention

We do not run servers and keep no copy of your data. Game progress stays in the game's private storage on your device until you delete it. Information processed by Google or Unity is kept according to their own retention rules.

## Your rights and choices

- **Remove ads:** buy Remove Ads in the game's settings.
- **Delete your data:** uninstall the game or open Android Settings, then Apps, Fruit Tree, Storage, Clear data.
- **Your rights:** depending on where you live, you can ask to access, correct or delete your data, restrict or object to its processing, and complain to your local data protection authority.
- **Questions:** write to us at the email below. We aim to reply within 30 days.

## Changes to this policy

If the game's features or the services it uses change, we will update this policy and the "Last updated" date above. The current version is always available at the same address.

## Contact
TheLightCome  
Email: nurikserikbay1995@gmail.com

---

## Как опубликовать (эта часть в Google Doc не копируется)

1. Документ уже создан и заполнен (ссылка в начале файла). При правках меняйте и документ, и этот файл.
2. Имя разработчика (TheLightCome) и email поддержки уже вписаны. Если в Play Console имя другое, поменяйте его в обоих местах.
3. **Файл → Поделиться → Опубликовать в интернете → Опубликовать.** Ссылка вида `https://docs.google.com/document/d/e/.../pub`. Проверьте её в режиме инкогнито.
4. Ссылку вставить в `Assets/Scripts/FruitTreePrivacyConfig.cs` (`PublishedPolicyUrl`) и в Play Console → App content → Privacy policy.
5. Data safety в Play Console должна совпадать с разделом «Advertising»: по раскрытию Google для Mobile Ads SDK отмечаются Approximate location (по IP), App interactions, Diagnostics и Device or other IDs (App Set ID). Цели: реклама, аналитика, защита от мошенничества. Данные шифруются при передаче и не продаются. Плюс история покупок через Google Play.
6. Если поменяются SDK, частота рекламы или появится персонализированная реклама, обновите политику до выпуска сборки.
