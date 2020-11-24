using System;
using System.ComponentModel;
using System.Data;
using System.Data.Linq;
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
    public EditorController([NotNull] IEditorView view,
      [NotNull] Type mainListType) {
      View = view;
      MainListType = mainListType;
      View.SetController(this);
    }

    /// <summary>
    ///   Gets metadata about the database columns
    ///   represented by the Entity's field properties.
    /// </summary>
    [NotNull]
    internal BindingColumnList Columns => MainList.Columns;

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

    protected virtual ChangeAction LastChangeAction =>
      MainList.LastDatabaseUpdateErrorException.ChangeAction;

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

    private bool IsDuplicateKeyException =>
      MainList.LastDatabaseUpdateErrorException?.InnerException is DuplicateKeyException;

    private bool IsFormatException =>
      MainList.LastDatabaseUpdateErrorException?.InnerException is FormatException;

    /// <summary>
    ///   Gets whether the current main grid row is the insertion row,
    ///   which is for adding new entities and is located at the bottom of the grid.
    /// </summary>
    public bool IsInsertionRowCurrent => MainList.IsInsertionRowCurrent;

    /// <summary>
    ///   Gets whether a read-only related grid for a parent table is to be shown
    ///   above the main grid.
    /// </summary>
    public bool IsParentTableToBeShown => ParentList?.BindingList != null;

    private bool IsReferencingValueNotFoundException =>
      MainList.LastDatabaseUpdateErrorException?.InnerException
        is RowNotInTableException;

    [CanBeNull] public IBindingList MainBindingList => MainList.BindingList;

    /// <summary>
    ///   Gets or set the list of entities represented in the main table.
    /// </summary>
    protected IEntityList MainList { get; private set; }

    [NotNull] private Type MainListType { get; }
    [CanBeNull] public string MainTableName => MainList.EntityTypeName;
    [CanBeNull] public IBindingList ParentBindingList => ParentList?.BindingList;

    /// <summary>
    ///   Gets or sets the list of entities represented in the parent table, if any.
    /// </summary>
    [CanBeNull]
    private IEntityList ParentList { get; set; }

    [NotNull] private IEditorView View { get; }

    /// <summary>
    ///   Returns whether the specified column references another entity.
    /// </summary>
    public bool DoesColumnReferenceAnotherEntity([NotNull] string columnName) {
      return !string.IsNullOrWhiteSpace(Columns[columnName].ReferencedPropertyName);
    }

    /// <summary>
    ///   Edit the tags of the audio file, if found,
    ///   of the current Piece, if any,
    ///   Otherwise shows an informative message box.
    /// </summary>
    /// <exception cref="ApplicationException">
    ///   Whatever error might be thrown on attempting to update the tags.
    /// </exception>
    [ExcludeFromCodeCoverage]
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

    public void ShowError() {
      Debug.WriteLine(
        $"EditorController.ShowError: LastChangeAction == {LastChangeAction}");
      View.FocusMainGridCell(MainList.LastDatabaseUpdateErrorException.RowIndex,
        MainList.LastDatabaseUpdateErrorException.ColumnIndex);
      if (LastChangeAction == ChangeAction.Delete) {
        View.SelectCurrentRowOnly();
      }
      View.ShowErrorMessage(MainList.LastDatabaseUpdateErrorException.Message);
      Debug.WriteLine("Error message shown");
      if (IsFormatException) {
        // if (LastChangeAction == ChangeAction.Insert) {
        //   // A FormatException was thrown due to an invalidly formatted paste
        //   // into an insertion row cell, e.g. text into a date cell.
        //   // This the only type of editing error
        //   // where the error row remains the insertion row.
        //   //
        //   // Unlike other editing error types,
        //   // including FormatException in an existing row cell,
        //   // in this case we are not returning edited cells to their changed values.
        //   //
        //   // It looks like that would be very tricky in to do in the insertion row.
        //   // It seems to be impossible to change insertion cell values programatically.
        //   // Perhaps the insertion row could to be exited and replaced with
        //   // a new one based on a special binding item pre-populated
        //   // with the changed values (apart from the property corresponding to the
        //   // FormatException, which would be impossible).
        //   // As this should be a rare scenario and the resulting lack of data restoration
        //   // is not expected to be seen as onerous by users,
        //   // I'm putting this in the too-hard basket for now.
        // } else {
        //   // LastChangeAction == ChangeAction.Update
        // }
        return;
      }
      if (IsDuplicateKeyException || IsReferencingValueNotFoundException) {
        if (LastChangeAction == ChangeAction.Update) {
          MainList.RestoreReferencingPropertyOriginalValue(
            MainList.LastDatabaseUpdateErrorException.RowIndex,
            MainList.LastDatabaseUpdateErrorException.ColumnIndex);
          return;
        }
      }
      switch (LastChangeAction) {
        case ChangeAction.Delete:
          break;
        case ChangeAction.Insert:
          OnInsertErrorAcknowledged();
          break;
        case ChangeAction.Update:
          MainList.RestoreCurrentBindingItemOriginalValues();
          View.EditMainGridCurrentCell();
          View.RestoreMainGridCurrentRowCellErrorValue(
            MainList.LastDatabaseUpdateErrorException.ColumnIndex,
            MainList.GetErrorValues()[
              MainList.LastDatabaseUpdateErrorException.ColumnIndex]);
          break;
        default:
          throw new NotSupportedException(
            $"{nameof(ChangeAction)} '{LastChangeAction}' is not supported.");
      }
    }

    [NotNull]
    protected virtual IEntityList CreateEntityList([NotNull] Type type) {
      return Global.CreateEntityList(type);
    }

    [ExcludeFromCodeCoverage]
    [NotNull]
    protected virtual Option CreateOption([NotNull] string name) {
      return new Option(name);
    }

    [NotNull]
    public string GetColumnDisplayName([NotNull] string columnName) {
      var column = Columns[columnName];
      return column.DisplayName ?? columnName;
    }

    public void OnExistingRowCellUpdateError(int rowIndex, [NotNull] string columnName,
      [CanBeNull] Exception exception) {
      switch (exception) {
        case DatabaseUpdateErrorException databaseUpdateErrorException:
          MainList.LastDatabaseUpdateErrorException = databaseUpdateErrorException;
          View.StartOnErrorTimer();
          break;
        case DuplicateKeyException duplicateKeyException:
          MainList.OnValidationError(rowIndex, columnName, duplicateKeyException);
          View.StartOnErrorTimer();
          break;
        case FormatException formatException:
          // An invalid value was pasted into a cell, e.g. text into a date.
          MainList.OnValidationError(rowIndex, columnName, formatException);
          //MainList.OnFormatException(rowIndex, columnName, formatException);
          View.StartOnErrorTimer();
          break;
        case RowNotInTableException referencedEntityNotFoundException:
          // A combo box cell value 
          // does not match any of it's embedded combo box's items.
          // So the combo box's selected index and text could not be updated.
          // As the combo boxes are all dropdown lists,
          // the only way this can have happened is that
          // the unmatched value was pasted into the cell.
          // If the cell value had been changed
          // by selecting an item on the embedded combo box,
          // it could only be a matching value.
          // MainList.OnReferencedEntityNotFound(rowIndex, columnName, 
          MainList.OnValidationError(rowIndex, columnName,
            referencedEntityNotFoundException);
          View.StartOnErrorTimer();
          break;
        case null:
          // For unknown reason, the way I've got the error handling set up,
          // this event gets raise twice if there's a DatabaseUpdateErrorException,
          // the second time with a null exception.
          // It does not seem to do any harm, so long as it is trapped like this.
          break;
        default:
          // Terminal error.  In the Release compilation,
          // the stack trace will be shown by the terminal error handler in Program.cs.
          throw exception;
      }
    }

    /// <summary>
    ///   Invoked when the user clicks OK on an insert error message box.
    ///   If the insertion row is not the only row,
    ///   the row above the insertion row is temporarily made current,
    ///   which allows the insertion row to be removed.
    ///   The insertion row is then removed,
    ///   forcing a new empty insertion row to be created.
    ///   Finally the new empty insertion row is made current.
    ///   The net effect is that, after the error message has been shown,
    ///   the insertion row remains current, from the perspective of the user,
    ///   but all its cells have been blanked out or, where applicable,
    ///   reverted to default values.
    /// </summary>
    /// <remarks>
    ///   The disadvantage is that the user's work on the insertion row is lost
    ///   and, if still needed, has to be restarted from scratch.
    ///   So why is this done?
    ///   <para>
    ///     If the insertion row were to be left with the error values,
    ///     the user would no way of cancelling out of the insertion
    ///     short of closing the application.
    ///     And, as the most probable or only error type is a duplicate key,
    ///     the user would quite likely want to cancel the insertion and
    ///     edit the existing row with that key instead.
    ///   </para>
    ///   <para>
    ///     There is no way of selectively reverting just the erroneous cell values
    ///     of an insertion row to blank or default.
    ///     So I spent an inordinate amount of effort trying to get
    ///     another option to work.
    ///     The idea was to convert the insertion row into an 'existing' row
    ///     without updating the database, resetting just the erroneous cell values
    ///     to blank or default.
    ///     While I still think that would be possible,
    ///     it turned out to add a great deal of complexity
    ///     just to get it not working reliably.
    ///     So I gave up and went for this relatively simple
    ///     and hopefully robust solution instead.
    ///   </para>
    /// </remarks>
    private void OnInsertErrorAcknowledged() {
      Debug.WriteLine("EditorController.OnInsertErrorAcknowledged");
      int insertionRowIndex = MainList.BindingList.Count - 1;
      if (insertionRowIndex > 0) {
        // Currently, it is not anticipated that there can be an insertion row error
        // at this point 
        // when the insertion row is the only row on the table,
        // as the only anticipated error type that should get to this point
        // is a duplicate key.
        // Format errors are handled differently and should not get here.
        View.MakeMainGridRowCurrent(insertionRowIndex - 1);
        MainList.RemoveInsertionBindingItem();
        View.MakeMainGridRowCurrent(insertionRowIndex);
      }
    }

    /// <summary>
    ///   A combo box cell value on the main grid's insertion row
    ///   does not match any of it's embedded combo box's items.
    ///   So the combo box's selected index and text could not be updated.
    ///   As the combo boxes are all dropdown lists,
    ///   the only way this can have happened is that
    ///   the unmatched value was pasted into the cell.
    ///   If the cell value had been changed
    ///   by selecting an item on the embedded combo box,
    ///   it could only be a matching value.
    /// </summary>
    internal void OnInsertionRowReferencedEntityNotFound(
      int rowIndex, [NotNull] string columnName,
      [NotNull] string simpleKey) {
      var referencedEntityNotFoundException =
        ReferenceableItemList.CreateReferencedEntityNotFoundException(
          columnName, simpleKey);
      MainList.OnValidationError(
        rowIndex, columnName, referencedEntityNotFoundException);
      View.StartOnErrorTimer();
    }

    public void OnMainGridRowEnter(int rowIndex) {
      // Debug.WriteLine(
      //   $"{nameof(OnMainGridRowEnter)}:  Any row entered (after ItemAdded if insertion row)");
      MainList.OnRowEnter(rowIndex);
    }

    /// <summary>
    ///   Deletes the entity at the specified row index
    ///   from the database and removes it from the list.
    /// </summary>
    public void OnMainGridRowRemoved(int rowIndex) {
      // Debug.WriteLine(
      //   $"{nameof(OnMainGridRowRemoved)}:  2 or 3 times on opening a table before 1st ItemAdded (insertion row entered); existing row removed");
      // For unknown reason, the grid's RowRemoved event is raised 2 or 3 times
      // while data is being loaded into the grid.
      // Also, the grid row might have been removed because of an insertion error,
      // in which case the entity will not have been persisted (rowIndex == Count).
      if (MainList.IsDataLoadComplete && rowIndex < MainList.Count) {
        try {
          MainList.DeleteEntity(rowIndex);
          View.OnRowAddedOrDeleted();
        } catch (DatabaseUpdateErrorException) {
          View.StartOnErrorTimer();
        }
      }
    }

    /// <summary>
    ///   If the specified table row is new,
    ///   inserts an entity on the database with the table row data.
    /// </summary>
    public void OnMainGridRowValidated(int rowIndex) {
      // Debug.WriteLine(
      //   $"{nameof(OnMainGridRowValidated)}:  Any row left, after final ItemChanged, if any");
      try {
        MainList.OnRowValidated(rowIndex);
        View.OnRowAddedOrDeleted();
      } catch (DatabaseUpdateErrorException) {
        View.StartOnErrorTimer();
      }
    }

    /// <summary>
    ///   An existing row on the parent grid has been entered.
    ///   So the main grid will be populated with the required
    ///   child entities of the entity at the specified row index.
    /// </summary>
    public void OnParentGridRowEntered(int rowIndex) {
      MainList.Populate(ParentList?.GetChildrenForMainList(rowIndex));
    }

    /// <summary>
    ///   Plays the audio, if found,
    ///   of the current Piece, if any.
    /// </summary>
    /// <exception cref="ApplicationException">
    ///   The audio cannot be played.
    /// </exception>
    [ExcludeFromCodeCoverage]
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

    public void ShowWarningMessage(string message) {
      View.ShowWarningMessage(message);
    }
  }
}