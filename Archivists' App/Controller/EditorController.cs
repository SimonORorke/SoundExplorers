using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using SoundExplorers.Model;
using NotNullAttribute = JetBrains.Annotations.NotNullAttribute;

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

    private Option _gridSplitterDistanceOption;
    private Option _imageSplitterDistanceOption;

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
    public EditorController([NotNull] IEditorView view,
      [NotNull] Type mainListType, [NotNull] MainController mainController) {
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
      _gridSplitterDistanceOption ?? (_gridSplitterDistanceOption =
        CreateOption($"{MainList.EntityTypeName}.GridSplitterDistance"));

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
      _imageSplitterDistanceOption ?? (_imageSplitterDistanceOption =
        new Option($"{MainList.EntityTypeName}.ImageSplitterDistance"));

    public bool IsClosing { get; set; }

    /// <summary>
    ///   Gets whether a read-only related grid for a parent table is to be shown
    ///   above the main grid.
    /// </summary>
    public bool IsParentTableToBeShown => ParentList?.BindingList != null;

    [NotNull] internal MainController MainController { get; }

    /// <summary>
    ///   Gets or set the list of entities represented in the main table.
    /// </summary>
    internal IEntityList MainList { get; private set; }

    [NotNull] private Type MainListType { get; }
    [CanBeNull] public IBindingList ParentBindingList => ParentList?.BindingList;

    /// <summary>
    ///   Gets or sets the list of entities represented in the parent table, if any.
    /// </summary>
    [CanBeNull]
    internal IEntityList ParentList { get; set; }

    [NotNull] private IEditorView View { get; }

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

    public void FetchData() {
      MainList = CreateEntityList(MainListType);
      if (MainList.ParentListType != null) {
        ParentList = CreateEntityList(MainList.ParentListType);
        ParentList.IsParentList = true;
        ParentList.Populate();
        if (ParentList.BindingList?.Count > 0) {
          MainList.Populate(ParentList.GetChildrenForMainList(0));
        }
      } else {
        MainList.Populate();
      }
    }

    // public void ShowError() {
    //   // Debug.WriteLine(
    //   //   $"EditorController.ShowError: LastChangeAction == {LastChangeAction}");
    //   View.MakeCellCurrent(MainList.LastDatabaseUpdateErrorException.RowIndex,
    //     MainList.LastDatabaseUpdateErrorException.ColumnIndex);
    //   if (LastChangeAction == StatementType.Delete) {
    //     View.SelectCurrentRowOnly();
    //   }
    //   View.ShowErrorMessage(MainList.LastDatabaseUpdateErrorException.Message);
    //   // Debug.WriteLine("Error message shown");
    //   if (IsFormatException) {
    //     return;
    //   }
    //   if (IsDuplicateKeyException || IsReferencingValueNotFoundException) {
    //     if (LastChangeAction == StatementType.Update) {
    //       MainList.RestoreReferencingPropertyOriginalValue(
    //         MainList.LastDatabaseUpdateErrorException.RowIndex,
    //         MainList.LastDatabaseUpdateErrorException.ColumnIndex);
    //       return;
    //     }
    //   }
    //   switch (LastChangeAction) {
    //     case StatementType.Delete:
    //       break;
    //     case StatementType.Insert:
    //       CancelInsertion();
    //       break;
    //     case StatementType.Update:
    //       MainList.RestoreCurrentBindingItemOriginalValues();
    //       View.EditCurrentCell();
    //       View.RestoreMainGridCurrentRowCellErrorValue(
    //         MainList.LastDatabaseUpdateErrorException.ColumnIndex,
    //         MainList.GetErrorValues()[
    //           MainList.LastDatabaseUpdateErrorException.ColumnIndex]);
    //       break;
    //     default:
    //       throw new NotSupportedException(
    //         $"{nameof(ChangeAction)} '{LastChangeAction}' is not supported.");
    //   }
    // }

    // /// <summary>
    // ///   Invoked when the user clicks OK on an insert error message box.
    // ///   If the insertion row is not the only row,
    // ///   the row above the insertion row is temporarily made current,
    // ///   which allows the insertion row to be removed.
    // ///   The insertion row is then removed,
    // ///   forcing a new empty insertion row to be created.
    // ///   Finally the new empty insertion row is made current.
    // ///   The net effect is that, after the error message has been shown,
    // ///   the insertion row remains current, from the perspective of the user,
    // ///   but all its cells have been blanked out or, where applicable,
    // ///   reverted to default values.
    // /// </summary>
    // /// <remarks>
    // ///   The disadvantage is that the user's work on the insertion row is lost
    // ///   and, if still needed, has to be restarted from scratch.
    // ///   So why is this done?
    // ///   <para>
    // ///     If the insertion row were to be left with the error values,
    // ///     the user would no way of cancelling out of the insertion
    // ///     short of closing the application.
    // ///     And, as the most probable or only error type is a duplicate key,
    // ///     the user would quite likely want to cancel the insertion and
    // ///     edit the existing row with that key instead.
    // ///   </para>
    // ///   <para>
    // ///     There is no way of selectively reverting just the erroneous cell values
    // ///     of an insertion row to blank or default.
    // ///     So I spent an inordinate amount of effort trying to get
    // ///     another option to work.
    // ///     The idea was to convert the insertion row into an 'existing' row
    // ///     without updating the database, resetting just the erroneous cell values
    // ///     to blank or default.
    // ///     While I still think that would be possible,
    // ///     it turned out to add a great deal of complexity
    // ///     just to get it not working reliably.
    // ///     So I gave up and went for this relatively simple
    // ///     and hopefully robust solution instead.
    // ///   </para>
    // ///   <para>
    // ///     If in future we want to allow the insertion row to be re-edited,
    // ///     perhaps the insertion row could to be replaced with
    // ///     a new one based on a special binding item pre-populated
    // ///     with the changed values (apart from the property corresponding to a
    // ///     FormatException, which would be impossible,
    // ///     or a reference not found paste error, which would be pointless).
    // ///   </para>
    // /// </remarks>
    // private void CancelInsertion() {
    //   // Debug.WriteLine("EditorController.CancelInsertion");
    //   int insertionRowIndex = MainList.BindingList.Count - 1;
    //   if (insertionRowIndex > 0) {
    //     // Currently, it is not anticipated that there can be an insertion row error
    //     // at this point 
    //     // when the insertion row is the only row on the table,
    //     // as the only anticipated error type that should get to this point
    //     // is a duplicate key.
    //     // Format errors are handled differently and should not get here.
    //     MainList.IsRemovingInvalidInsertionRow = true;
    //     View.MakeRowCurrent(insertionRowIndex - 1);
    //     MainList.RemoveInsertionBindingItem();
    //     View.MakeRowCurrent(insertionRowIndex);
    //   }
    // }

    [NotNull]
    protected virtual IEntityList CreateEntityList([NotNull] Type type) {
      return Global.CreateEntityList(type);
    }

    [ExcludeFromCodeCoverage]
    [NotNull]
    protected virtual Option CreateOption([NotNull] string name) {
      return new Option(name);
    }

    /// <summary>
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