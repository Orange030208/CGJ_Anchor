mergeInto(LibraryManager.library, {
  $LeaderboardNicknameInput: {
    field: null,
    observer: null,
    bounds: null,

    place: function () {
      var field = LeaderboardNicknameInput.field;
      var bounds = LeaderboardNicknameInput.bounds;
      if (!field || !bounds) return;

      var canvas = Module.canvas.getBoundingClientRect();
      var parent = field.offsetParent;
      if (!parent) return;
      var origin = parent.getBoundingClientRect();
      field.style.left = (canvas.left - origin.left + parent.scrollLeft - parent.clientLeft + canvas.width * bounds[0]) + 'px';
      field.style.top = (canvas.top - origin.top + parent.scrollTop - parent.clientTop + canvas.height * bounds[1]) + 'px';
      field.style.width = (canvas.width * bounds[2]) + 'px';
      field.style.height = (canvas.height * bounds[3]) + 'px';
      field.style.fontSize = Math.max(16, canvas.height * bounds[4]) + 'px';
    }
  },

  OpenLeaderboardNicknameInput__deps: ['$LeaderboardNicknameInput'],
  OpenLeaderboardNicknameInput: function (target, value, maxLength) {
    if (LeaderboardNicknameInput.field) return;

    var objectName = UTF8ToString(target);
    var field = document.createElement('input');
    field.type = 'text';
    field.value = UTF8ToString(value);
    field.setAttribute('aria-label', '排行榜昵称');
    field.autocomplete = 'off';
    field.spellcheck = false;
    if (maxLength > 0) field.maxLength = maxLength;
    field.style.cssText = 'position:absolute;z-index:4;box-sizing:border-box;margin:0;padding:0;border:0;outline:0;' +
      'border-radius:0;background:transparent;color:transparent;-webkit-text-fill-color:transparent;' +
      'caret-color:#fff;font-family:monospace;';

    // Native browser text input handles CJK composition; Unity TMP draws the value.
    function mirror() {
      SendMessage(objectName, 'SetBrowserNickname', field.value);
    }

    var composing = false;
    field.addEventListener('input', mirror);
    field.addEventListener('compositionstart', function () { composing = true; });
    field.addEventListener('compositionend', function () {
      composing = false;
      mirror();
    });
    field.addEventListener('keydown', function (event) {
      if (event.key === 'Enter' && !composing && !event.isComposing && event.keyCode !== 229) {
        event.preventDefault();
        mirror();
        SendMessage(objectName, 'SubmitBrowserNickname');
      }
    });

    Module.canvas.parentElement.appendChild(field);
    LeaderboardNicknameInput.field = field;
    if (typeof ResizeObserver === 'function') {
      LeaderboardNicknameInput.observer = new ResizeObserver(LeaderboardNicknameInput.place);
      LeaderboardNicknameInput.observer.observe(Module.canvas);
    }
  },

  PositionLeaderboardNicknameInput__deps: ['$LeaderboardNicknameInput'],
  PositionLeaderboardNicknameInput: function (x, y, width, height, fontSize) {
    LeaderboardNicknameInput.bounds = [x, y, width, height, fontSize];
    LeaderboardNicknameInput.place();
  },

  CloseLeaderboardNicknameInput__deps: ['$LeaderboardNicknameInput'],
  CloseLeaderboardNicknameInput: function () {
    var field = LeaderboardNicknameInput.field;
    if (!field) return;

    if (LeaderboardNicknameInput.observer) LeaderboardNicknameInput.observer.disconnect();
    var hadFocus = document.activeElement === field;
    field.remove();
    if (hadFocus) Module.canvas.focus();
    LeaderboardNicknameInput.field = null;
    LeaderboardNicknameInput.observer = null;
    LeaderboardNicknameInput.bounds = null;
  }
});
