# Fruit Tree — Privacy Policy
**DRAFT — NOT FOR PUBLICATION UNTIL CONTACT DETAILS AND SDK DATA AUDIT ARE COMPLETE**

Last updated: 12 October 2026

**App:** Fruit Tree (`com.nurgeldiserikbay.fruittree`)
**Developer / publisher:** [REPLACE WITH EXACT GOOGLE PLAY DEVELOPER DISPLAY NAME]
**Privacy contact:** [REPLACE WITH WORKING PUBLIC SUPPORT EMAIL]

## Overview

Fruit Tree is an offline-first, relaxing 3D fruit-picking game. This notice explains how the app handles data, including local gameplay progress and optional Google Play purchases. We aim to minimize data processing, especially because the game may be played by children.

## Game progress stored on your device

Fruit Tree stores tree-selection preferences, collected fruit identifiers, unlocked tree progress, round completions, and sound settings locally on your device (`Application.persistentDataPath` and Unity PlayerPrefs). This data is used to resume gameplay and preserve your tree collection. The developer does not operate a Fruit Tree account service or a backend database for this information. Local data may be removed when you uninstall the app or clear its storage; device backups may depend on your Android settings.

## Payments and third-party services

The app includes an optional Google Play Billing / Unity In-App Purchasing integration for a one-time `remove_ads` product. The developer does not process payment card information directly. Purchase availability, payment, restoration, and related purchase metadata may be processed by Google Play and Unity IAP as needed to provide the purchase service. The Remove Ads control is disabled until a compliant ads service exists.

**Before release:** review the actual packaged versions and privacy disclosures of Unity IAP, Google Play services and any other included SDK, and update this section and the Play Console Data safety form. Do not claim that the app collects absolutely no information without an SDK/network audit.

## Advertising and analytics

At the time this draft was authored, the Fruit Tree project had **no advertisement SDK integrated**. Its optional ad-access policy does not itself display ads. No separate advertising service is currently connected.

**If an advertising or analytics SDK is added**, revise this policy to disclose relevant partners, data categories, purposes, sharing and any child-directed restrictions **before enabling that functionality**. The final Play Console disclosures must match the actual built application.

## Children's privacy

Fruit Tree is designed to be calm and accessible to younger players. It does not require an in-game account, name, chat, or location permission. An area containing potential purchases is behind a parent-oriented hold interaction; this is not a reliable age-verification mechanism. Third-party payments and advertising, if activated, must follow the applicable Google Play Families requirements.

## Security, retention and deletion

Gameplay progress remains in local app storage until the user deletes it through device settings or removes the app, subject to Android backup behaviour. The developer does not receive a central copy of that progress in the current backend-free design. Purchase transaction information handled by Google Play is subject to Google's retention and legal obligations. If you have questions, contact the address above.

## Changes to this policy

We may update this notice when game features, libraries, advertising, or data handling change. We will update the date and keep the current public version accessible from the game and its Google Play listing.

## Contact

Developer: [REPLACE WITH EXACT PUBLISHER NAME]
Email: [REPLACE WITH PUBLIC SUPPORT EMAIL]

---
## Публикациялау тәртібі (құжаттың жария нұсқасына кірмейді)

1. Google Docs-та жаңа құжат ашып, жоғарыдағы **ағылшынша саясат мәтінін ғана** көшіріңіз; төмендегі нұсқаулықты көшірмеңіз.
2. Барлық `[REPLACE ...]` орындарын нақты Play Console-дегі баспагер атауымен және жұмыс істейтін қолдау email-імен алмастырыңыз.
3. Release APK/AAB құрамындағы барлық SDK, permissions және дерек ағынын тексеріп, саясат пен Play Console Data safety мәлімдемесін сәйкестендіріңіз.
4. **File → Share → Publish to web → Publish** таңдаңыз. Пайда болған жария `https://docs.google.com/document/d/e/.../pub` сілтемесін инкогнито браузерде кірмей ашып тексеріңіз.
5. Сол нақты сілтемені `Assets/Scripts/FruitTreePrivacyConfig.cs` ішіндегі `PublishedPolicyUrl` жолына және Play Console → Store presence / App content → Privacy policy URL өрісіне енгізіңіз.
6. Құжатты үнемі қолжетімді, өңдеуге жабық күйде ұстаңыз. Жария Google Docs URL болмаса онлайн батырма әдейі белсенді емес.
7. Google Docs жариялануын тексеріп, жаңа Unity сахнасын қайта құрастырыңыз. Адамға көрінбейтін/жарамсыз URL-мен Store release жасамаңыз.

**Google Play ресми талаптары:**
- https://support.google.com/googleplay/android-developer/answer/10144311
- https://support.google.com/docs/answer/183965
