using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using SoundExplorers.Model;

namespace SoundExplorers.Controller {
  /// <summary>
  ///   Controller for the table editor.
  /// </summary>
  [UsedImplicitly]
  public class EditorController {
    /// <summary>
    ///   Gets the format in which dates are to be shown on the grid.
    /// </summary>
    public const string DateFormat = Global.DateFormat;

    private Option? _gridSplitterDistanceOption;
    private Option? _imageSplitterDistanceOption;
    private IEntityList? _mainList;
    private IEntityList? _parentList;

    /// <summary>
    ///   Initialises a new instance of the <see cref="EditorController" /> class.
    /// </summary>
    /// <param name="view">
    ///   The table editor view to be shown.
    /// </param>
    /// <param name="mainListType">
    ///   The type of entity list whose data is to be displayed.
    /// </param>
    /// <param name="mainController">
    ///   Controller for the main window.
    /// </param>
    public EditorController(IEditorView view,
      Type mainListType, MainController mainController) {
      View = view;
      MainListType = mainListType;
      MainController = mainController;
      View.SetController(this);
    }

    /// <summary>
    ///   User option for the position of the split between the
    ///   (upper) parent grid, if shown, and the (lower) main grid.
    /// </summary>
    public int GridSplitterDistance {
      get => GridSplitterDistanceOption.Int32Value;
      set => GridSplitterDistanceOption.Int32Value = value;
    }

    private Option GridSplitterDistanceOption =>
      _gridSplitterDistanceOption ??=
        CreateOption($"{MainList.EntityTypeName}.GridSplitterDistance");

    /// <summary>
    ///   User option for the position of the split between the
    ///   image data (above) and the image (below) in the image table editor.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public int ImageSplitterDistance {
      get => ImageSplitterDistanceOption.Int32Value;
      set => ImageSplitterDistanceOption.Int32Value = value;
    }

    [ExcludeFromCodeCoverage]
    private Option ImageSplitterDistanceOption =>
      _imageSplitterDistanceOption ??=
        new Option($"{MainList.EntityTypeName}.ImageSplitterDistance");

    public bool IsClosing { get; set; }

    /// <summary>
    ///   Gets whether a read-only related grid for a parent table is to be shown above
    ///   the main grid.
    /// </summary>
    public bool IsParentGridToBeShown => MainList.ParentListType != null;

    internal bool IsPopulating { get; private set; }
    private bool IsRefreshingData { get; set; }
    internal MainController MainController { get; }
    public IMainGrid MainGrid { get; set; } = null!;
    public IParentGrid ParentGrid { get; set; } = null!;

    /// <summary>
    ///   Gets the list of entities represented in the main table.
    /// </summary>
    internal IEntityList MainList => _mainList ??= CreateEntityList(MainListType);

    private Type MainListType { get; }
    public IBindingList? ParentBindingList => ParentList?.BindingList;

    /// <summary>
    ///   Gets the list of entities represented in the parent table, if any.
    /// </summary>
    internal IEntityList? ParentList => IsParentGridToBeShown
      ? _parentList ??= CreateEntityList(
        MainList.ParentListType ??
        throw new InvalidOperationException(
          "MainList.ParentListType is unexpectedly null."))
      : null;

    internal IEditorView View { get; }

    /// <summary>
    ///   Edit the tags of the audio file, if found,
    ///   of the current Piece, if any,
    ///   Otherwise shows an informative message box.
    /// </summary>
    /// <exception cref="ApplicationException">
    ///   Whatever error might be thrown on attempting to update the tags.
    /// </exception>
    [ExcludeFromCodeCoverage]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public void EditAudioFileTags() {
      // string path = GetMediumPath(Medium.Audio);
      // var dummy = new AudioFile(path);
    }

    protected virtual IEntityList CreateEntityList(Type type) {
      return Global.CreateEntityList(type);
    }

    [ExcludeFromCodeCoverage]
    protected virtual Option CreateOption(string name) {
      return new(name);
    }

    public void OnMainGridPopulatedAsync() {
      Debug.WriteLine("EditorController.OnMainGridPopulatedAsync");
      // It is worth checking to see whether a grid needs to be focused: when a parent
      // grid is shown, focusing a grid involves additional processing; and it is more
      // logical for unit testing.
      if (IsParentGridToBeShown) {
        if (!ParentGrid.Focused) {
          ParentGrid.Focus();
        }
      } else { // No parent grid
        if (!MainGrid.Focused) {
          MainGrid.Focus();
        }
      }
    }

    public void OnParentAndMainGridsShownAsync() {
      Debug.WriteLine("EditorController.OnParentAndMainGridsShownAsync");
      if (ParentGrid.RowCount > 0) {
        ParentGrid.MakeRowCurrent(ParentGrid.RowCount - 1);
      }
      OnPopulatedAsync();
    }

    public void OnPopulatedAsync() {
      Debug.WriteLine("EditorController.OnPopulatedAsync");
      IsPopulating = false;
      if (MainGrid.RowCount > 0) {
        MainGrid.MakeRowCurrent(MainGrid.RowCount - 1);
      }
      View.SetCursorToDefault();
    }

    public void Populate() {
      Debug.WriteLine("EditorController.Populate");
      IsPopulating = true;
      if (IsParentGridToBeShown) {
        ParentGrid.RestoreCellColorSchemeToDefault();
        MainGrid.InvertCellColorScheme();
        ParentGrid.Populate();
        if (ParentGrid.RowCount > 0) {
          MainGrid.Populate(ParentGrid.Controller.GetChildrenForMainList(
            ParentGrid.RowCount - 1));
          // If the editor window is being loaded, the parent grid's current row is set
          // asynchronously in OnParentAndMainGridsShownAsync to ensure that it is
          // scrolled into view. Otherwise, i.e. if the grid contents are being
          // refreshed, we need to do it here.
          if (IsRefreshingData) {
            ParentGrid.MakeRowCurrent(ParentGrid.RowCount - 1);
            View.OnPopulated();
          } else { // Showing for first time
            View.OnParentAndMainGridsShown();
          }
        }
      } else {
        MainGrid.Populate();
        View.OnPopulated();
      }
      Debug.WriteLine("EditorController.Populate END");
    }

    public void PopulateMainGridOnParentRowChanged(int parentRowIndex) {
      Debug.WriteLine(
        $"EditorController.PopulateMainGridOnParentRowChanged: parent row {parentRowIndex}");
      IsPopulating = true;
      MainGrid.Populate(ParentGrid.Controller.GetChildrenForMainList(parentRowIndex));
      View.OnPopulated();
    }

    /// <summary>
    ///   Refreshes the contents of the existing grid or grids from the database.
    /// </summary>
    public void RefreshDataAsync() {
      IsRefreshingData = true;
      Populate();
      View.Refresh();
      IsRefreshingData = false;
    }

    /// <summary>
    ///   THE FOLLOWING RELATES TO A FEATURE THAT IS NOT YET IN USE BUT MAY BE LATER:
    ///   Plays the audio, if found,
    ///   of the current Piece, if any.
    /// </summary>
    /// <exception cref="ApplicationException">
    ///   The audio cannot be played.
    /// </exception>
    [ExcludeFromCodeCoverage]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public void PlayAudio() {
      //Process.Start(GetMediumPath(Medium.Audio));
    }

    /// <summary>
    ///   THE FOLLOWING RELATES TO A FEATURE THAT IS NOT YET IN USE BUT MAY BE LATER:
    ///   Plays the video, if found,
    ///   of the current Piece, if any.
    /// </summary>
    /// <exception cref="ApplicationException">
    ///   The video cannot be played.
    /// </exception>
    [ExcludeFromCodeCoverage]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public void PlayVideo() {
      //Process.Start(GetMediumPath(Medium.Video));
    }

    /// <summary>
    ///   THE FOLLOWING RELATES TO A FEATURE THAT IS NOT YET IN USE BUT MAY BE LATER:
    ///   Shows the newsletter, if any, associated with the current row.
    /// </summary>
    /// <exception cref="ApplicationException">
    ///   A newsletter cannot be shown.
    /// </exception>
    [ExcludeFromCodeCoverage]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public void ShowNewsletter() {
      // var newsletter = GetNewsletterToShow();
      // if (string.IsNullOrWhiteSpace(newsletter.Path)) { } else if (!File.Exists(
      //   newsletter.Path)) {
      //   throw new ApplicationException("Newsletter file \"" + newsletter.Path
      //     + "\", specified by the Path of the "
      //     + newsletter.Date.ToString(BindingColumn.DateFormat)
      //     + " Newsletter, cannot be found.");
      // } else {
      //   Process.Start(newsletter.Path);
      // }
    }
  }
}