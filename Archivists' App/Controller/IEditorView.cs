﻿namespace SoundExplorers.Controller {
  public interface IEditorView : IView<EditorController> {
    IGrid? CurrentGrid { get; }
    IMainGrid MainGrid { get; }
    IParentGrid ParentGrid { get; }
    void Close();
    void OnError();
    void OnParentAndMainGridsShown();
    // void PopulateMainGridOnParentRowChanged(int parentRowIndex);
    void SetMouseCursorToDefault();
    void SetMouseCursorToWait();
    void ShowErrorMessage(string text);
    void ShowWarningMessage(string text);
  }
}