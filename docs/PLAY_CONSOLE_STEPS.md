# Fruit Tree — шаги в Google Play Console

Всё, что нужно загрузить и вставить, уже готово. Идите по порядку.

## Файлы

| Что | Где |
|---|---|
| Сборка для закрытого теста (только тестовая реклама) | `Builds/FruitTree-testing.aab`, versionCode 1 |
| Иконка 512×512 | `Builds/store-art/icon-512.png` |
| Баннер 1024×500 (Feature graphic) | `Builds/store-art/feature-graphic-1024x500.jpg` |
| Скриншоты телефона 1080×1920, по порядку | `Builds/store-art/screenshot-1.jpg` … `screenshot-8.jpg` |

Картинки годятся и для продакшна. Пересобрать после изменений графики: Unity `-executeMethod FruitTree.Editor.StoreArtCapture.Run` (рендер в `Builds/store-art/raw`), затем `tools/StoreArtCompose.ps1`.

## 1. Создать приложение

All apps → Create app:
- App name: **Fruit Tree**
- Default language: **English (United States) – en-US**
- App or game: **Game**; Free or paid: **Free**
- Отметить согласия (Developer Program Policies, US export laws).

## 2. Store listing (Grow → Store presence → Main store listing)

- **App name:** Fruit Tree
- **Short description** (до 80 символов):

  ```
  Relax, rotate beautiful 3D fruit trees and pick every last fruit.
  ```

- **Full description:**

  ```
  Welcome to Fruit Tree, a calm 3D fruit-picking game for the whole family.

  Rotate each tree, look between the branches and gently pick ripe fruit, one by one. Pick every fruit to unlock the next tree, and come back to any tree you have cleared: it can bloom again whenever you like.

  • 10 fruit trees: apple, pear, cherry, peach, orange, apricot, plum, lemon, mandarin and pomegranate
  • 8 peaceful landscapes, from a quiet meadow to a lakeside and a Japanese garden
  • Changing weather: sunshine, mist, soft rain and distant thunder
  • Calm music, birdsong and a gentle sound for every fruit
  • A friendly hint helps you find the last few fruits
  • No timers, no lives, no pressure
  • Works offline; progress is saved on your device

  Fruit Tree contains ads that are suitable for children. A one-time "Remove Ads" purchase, protected by a parent check, turns them off.

  Take your time. The garden can wait.
  ```

- **App icon:** `icon-512.png`; **Feature graphic:** `feature-graphic-1024x500.jpg`
- **Phone screenshots:** `screenshot-1.jpg` … `screenshot-8.jpg`, в этом порядке.
- Store settings: Category **Games → Casual**; Tags: Casual, Relaxing, Family; Contact email: ваш gmail; Website можно оставить пустым.

## 3. App content (Policy → App content)

| Раздел | Ответ |
|---|---|
| Privacy policy | `https://docs.google.com/document/d/e/2PACX-1vSbaN-x4nfp2DgDITNpbkl17oiKkbPX0VMRy0mjApYHSoc1EjCBBO3O8mFG7Qo5gRuCyv5gwLidJ9xV/pub` |
| App access | All functionality is available without special access |
| Ads | **Yes, my app contains ads** |
| Advertising ID | **No** — разрешение AD_ID удалено из сборки |
| Content rating | Анкета IARC: категория Game; насилия, страха, азартных игр, общения игроков, обмена данными и покупок цифровых товаров нет, **кроме** in-app purchase (да). Ожидается рейтинг для всех возрастов |
| Target audience and content | Возрастные группы: можно отметить все, включая младше 13. Это включает программу Families: реклама AdMob уже настроена как детская (G, без персонализации), покупка за родительской проверкой |
| Appeals to children | Да, игра рассчитана в том числе на детей |
| News app / COVID / Government / Financial / Health | Нет |
| Data safety | См. ниже |

### Data safety

- Does your app collect or share user data? **Yes**
- All data encrypted in transit? **Yes**; Way to request deletion? данные игры хранятся только на устройстве — ответить **No** (аккаунтов нет; удаляется вместе с приложением).
- Собираемые данные (все: **Collected, Shared**, обработка не обязательная для пользователя — **required**, цели: **Advertising or marketing, Analytics, Fraud prevention, security and compliance**):
  - **Location → Approximate location** (IP-адрес, через AdMob)
  - **App activity → App interactions** (AdMob)
  - **App info and performance → Diagnostics** (AdMob)
  - **Device or other IDs** (App Set ID, AdMob)
- **Financial info → Purchase history**: Collected, не Shared, цель **App functionality** (покупка Remove Ads через Google Play).

## 4. Товар Remove Ads (Monetize → Products → In-app products)

- Product ID: **`remove_ads`** — точно так, маленькими буквами.
- Name: Remove Ads; Description: Turns off all ads in Fruit Tree. One-time purchase.
- Цену выберите сами, затем **Activate**.

## 5. Закрытое тестирование (Test and release → Testing → Closed testing)

1. Create track (или используйте «Closed testing – Alpha»).
2. Testers: создайте список email минимум из **12 человек**, сохраните ссылку для участия и разошлите её.
3. Countries: выберите страны.
4. Create release → загрузите `FruitTree-testing.aab`. При первой загрузке согласитесь на **Play App Signing** (Google хранит ключ подписи приложения, наш ключ TheLightCome становится ключом загрузки).
5. Release name: `1.0 (1) test ads`; Release notes:

   ```
   <en-US>
   First test build. Ads in this build are Google test ads.
   </en-US>
   ```

6. Review release → Start rollout. После проверки Google тестировщики смогут установить игру. Тест должен идти **14 дней** с 12+ активными участниками, прежде чем откроется заявка на продакшн.
7. License testing (Settings → License testing): добавьте свой gmail, чтобы проверять покупку без списания денег.

## 6. После закрытого теста — продакшн

1. Скажите мне — я соберу **Production AAB** (настоящая реклама) с **versionCode 2**.
2. Apply for production → загрузите его в Production.
3. В AdMob: после публикации свяжите приложение со страницей в Google Play (Apps → App settings → Add app store details); проверьте, что Max ad content rating = G.
