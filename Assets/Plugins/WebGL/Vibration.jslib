mergeInto(LibraryManager.library, {

  // Tarayıcının Vibration API'sini çağırır (Android Chrome destekler, iOS Safari desteklemez).
  JS_Vibrate: function (durationMs) {
    if (navigator.vibrate) {
      navigator.vibrate(durationMs);
    }
  },

  // Mobil tarayıcı mı? 1 = evet, 0 = hayır.
  JS_IsMobileBrowser: function () {
    return /Android|iPhone|iPad|iPod/i.test(navigator.userAgent) ? 1 : 0;
  }

});
