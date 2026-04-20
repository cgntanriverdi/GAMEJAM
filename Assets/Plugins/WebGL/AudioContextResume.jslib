mergeInto(LibraryManager.library, {
  ResumeWebAudioContext: function () {
    if (typeof WEBAudio !== "undefined" &&
        WEBAudio.audioContext &&
        WEBAudio.audioContext.state === "suspended") {
      WEBAudio.audioContext.resume();
    }
  }
});
