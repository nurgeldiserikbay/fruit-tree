# Fruit Tree — Privacy Policy

Last updated: 26 November 2026

**App:** Fruit Tree (Android, package `com.nurgeldiserikbay.fruittree`)
**Developer:** [DEVELOPER NAME AS SHOWN ON GOOGLE PLAY]
**Contact:** [SUPPORT EMAIL]

## 1. Summary

Fruit Tree is a calm, offline 3D game about picking fruit from trees. It is made for players of all ages, including children. The game has no accounts, no chat and no sign-in, and it does not ask for your name, contacts, photos or location. Your game progress stays on your device. The game shows ads from Google AdMob that are set up for children: they are not personalised and are limited to family-friendly content. You can remove ads with a one-time purchase through Google Play.

## 2. Information stored on your device

The game saves the following on your device only:

- which trees you have unlocked and which fruit you have picked;
- your best results and round progress;
- settings such as sound and music volume, graphics quality and reduced motion;
- how many tree changes have passed since the last ad, so ads are not shown too often;
- whether you have bought Remove Ads.

The developer does not run servers for this game and does not receive a copy of this information. It is deleted when you uninstall the game or clear its data in Android settings. Android may include it in your own device backup, depending on your settings.

## 3. Advertising (Google AdMob)

The free version shows a full-screen ad from time to time, at most once every three tree changes. Ads are provided by Google AdMob, a service of Google LLC.

Every ad request from Fruit Tree is marked as directed to children and to users under the age of consent, and is limited to content rated G (suitable for all audiences). As a result:

- ads are not personalised and are not based on your interests or activity in other apps;
- the game does not use the Android Advertising ID (the AD_ID permission is removed from the app);
- AdMob may still process limited technical information that is needed to deliver ads, count ad views, limit how often ads appear and prevent fraud. This can include your IP address (used to estimate a general location such as the country), device model and operating system version, language, app version, an app-specific identifier (App Set ID), and whether an ad was shown or tapped.

Google processes this information under its own policies. More information: https://policies.google.com/privacy and https://policies.google.com/technologies/partner-sites

## 4. Purchases (Google Play)

The game offers one optional, one-time purchase, "Remove Ads". Payment is handled entirely by Google Play. The developer never sees or stores your card or payment details. The game receives only a confirmation from Google Play that the purchase was made, so it can turn off ads and restore the purchase on a new device. Purchases are processed through Google Play Billing and the Unity In-App Purchasing library. Google's privacy policy applies to the payment: https://policies.google.com/privacy

The purchase button is placed behind a grown-up check (press and hold) so that young children do not open it by accident.

## 5. Game engine

Fruit Tree is built with the Unity engine. Unity may collect limited technical information about the device and app performance as described in Unity's privacy policy: https://unity.com/legal/privacy-policy

## 6. Children

Fruit Tree is suitable for children and follows the Google Play Families Policy. We do not knowingly collect personal information from children. The game has no accounts, chat, social features or links that let children share personal information. The only third-party service that receives data is Google AdMob, configured for child-directed treatment as described above, plus Google Play for an optional purchase.

If you are a parent or guardian and believe your child's information was collected, please contact us at the email below, and we will help.

## 7. Sharing and selling

We do not sell personal information. We do not share information with anyone except Google (AdMob and Google Play) and Unity, as described above, to make the game and its ads and purchases work.

## 8. Security and retention

Game progress stays in the game's private storage on your device until you delete it. Information processed by Google or Unity is kept according to their own retention rules.

## 9. Your choices

- **Remove ads:** buy Remove Ads in the game's settings.
- **Delete your data:** uninstall the game or open Android Settings → Apps → Fruit Tree → Storage → Clear data.
- **Questions:** write to us at the email below.

## 10. Changes to this policy

If the game's features or the services it uses change, we will update this policy and the "Last updated" date above. The current version is always available at the same address.

## 11. Contact

[DEVELOPER NAME AS SHOWN ON GOOGLE PLAY]
Email: [SUPPORT EMAIL]

---

## Как опубликовать (эта часть в Google Doc не копируется)

1. Создайте пустой Google Doc (https://docs.new) и пришлите ссылку: текст выше вставлю сам через подключение Google Docs. Или вставьте его вручную.
2. Замените `[DEVELOPER NAME AS SHOWN ON GOOGLE PLAY]` и `[SUPPORT EMAIL]` (по два раза) на имя разработчика из Play Console и рабочий email поддержки.
3. **Файл → Поделиться → Опубликовать в интернете → Опубликовать.** Ссылка вида `https://docs.google.com/document/d/e/.../pub`. Проверьте её в режиме инкогнито.
4. Ссылку вставить в `Assets/Scripts/FruitTreePrivacyConfig.cs` (`PublishedPolicyUrl`) и в Play Console → App content → Privacy policy.
5. Data safety в Play Console должна совпадать с разделом 3: собираются «Device or other IDs» и «App interactions» (для рекламы и аналитики рекламы, не продаются, шифруются при передаче), а также история покупок через Google Play.
6. Если поменяются SDK, частота рекламы или появится персонализированная реклама, обновите политику до выпуска сборки.
