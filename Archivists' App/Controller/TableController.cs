using System;
using System.Collections.Generic;
using System.Data;
using JetBrains.Annotations;
using SoundExplorers.Common;
using SoundExplorers.Model;

namespace SoundExplorers.Controller {
  /// <summary>
  ///   Controller for the table editor.
  /// </summary>
  [UsedImplicitly]
  public class TableController {
    private Option _gridSplitterDistanceOption;
    private Option _imageSplitterDistanceOption;

    /// <summary>
    ///   Initialises a new instance of the <see cref="TableController" /> class.
    /// </summary>
    /// <param name="view">
    ///   The table editor view to be shown.
    /// </param>
    /// <param name="mainListType">
    ///   The type of entity list whose data is to be displayed.
    /// </param>
    public TableController([NotNull] ITableView view,
      [NotNull] Type mainListType) {
      View = view;
      MainListType = mainListType;
      View.SetController(this);
    }

    /// <summary>
    ///   Gets metadata about the database columns
    ///   represented by the Entity's field properties.
    /// </summary>
    private EntityColumnList Columns => MainList?.Columns ??
                                        throw new NullReferenceException(
                                          nameof(Columns));

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
        new Option($"{MainTable?.TableName}.GridSplitterDistance"));

    /// <summary>
    ///   User option for the position of the split between the
    ///   image data (above) and the image (below) in the image table editor.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public int ImageSplitterDistance {
      get => ImageSplitterDistanceOption.Int32Value;
      set => ImageSplitterDistanceOption.Int32Value = value;
    }

    private Option ImageSplitterDistanceOption =>
      _imageSplitterDistanceOption ?? (_imageSplitterDistanceOption =
        new Option($"{MainTable?.TableName}.ImageSplitterDistance"));

    /// <summary>
    ///   Gets whether a read-only related grid for a parent table is to be shown
    ///   above the main grid.
    /// </summary>
    public bool IsParentTableToBeShown => ParentTable != null;

    /// <summary>
    ///   Gets or set the list of entities represented in the main table.
    /// </summary>
    [CanBeNull]
    private IEntityList MainList { get; set; }

    [NotNull] private Type MainListType { get; }
    [CanBeNull] public DataTable MainTable => MainList?.Table;

    /// <summary>
    ///   Gets or sets the list of entities represented in the parent table, if any.
    /// </summary>
    [CanBeNull]
    private IEntityList ParentList { get; set; }

    [CanBeNull] public DataTable ParentTable => ParentList?.Table;
    [NotNull] private ITableView View { get; }

    /// <summary>
    ///   Returns whether the specified column references another entity.
    /// </summary>
    public bool DoesColumnReferenceAnotherEntity([NotNull] string columnName) {
      return !string.IsNullOrEmpty(Columns[columnName]?.ReferencedColumnDisplayName);
    }

    /// <summary>
    ///   Edit the tags of the audio file, if found,
    ///   of the current Piece, if any,
    ///   Otherwise shows an informative message box.
    /// </summary>
    /// <exception cref="ApplicationException">
    ///   Whatever error might be thrown on attempting to update the tags.
    /// </exception>
    public void EditAudioFileTags() {
      // string path = GetMediumPath(Medium.Audio);
      // var dummy = new AudioFile(path);
    }

    public void FetchData() {
      MainList = Global.CreateEntityList(MainListType);
      if (MainList.ParentListType != null) {
        ParentList = Global.CreateEntityList(MainList.ParentListType);
        ParentList.IsParentList = true;
        ParentList.Populate();
        if (ParentList.Table.Rows.Count > 0) {
          MainList.Populate(ParentList.GetChildren(0));
        }
      } else {
        MainList.Populate();
      }
    }

    [NotNull]
    internal string GetReferencedColumnName([NotNull] string columnName) {
      return Columns[columnName]?.ReferencedColumnDisplayName ??
             throw new NullReferenceException("ReferencedParentColumnDisplayName");
    }

    [NotNull]
    internal Type GetReferencedEntityListType([NotNull] string columnName) {
      return MainList?.Columns[columnName]?.ReferencedEntityListType ??
             throw new NullReferenceException("ReferencedEntityListType");
    }

    [NotNull]
    internal string GetReferencedTableName([NotNull] string columnName) {
      return MainList?.Columns[columnName]?.ReferencedTableName ??
             throw new NullReferenceException("ReferencedTableName");
    }

    /// <summary>
    ///   If the specified table row is new or its data has changed,
    ///   inserts (if new) or updates corresponding the entity
    ///   on the database with the table row data.
    /// </summary>
    public void InsertOrUpdateEntityIfRequired(int rowIndex) {
      try {
        MainList?.InsertOrUpdateEntityIfRequired(rowIndex);
        View.OnRowUpdated();
      } catch (RowErrorException exception) {
        View.OnRowError(exception);
      }
    }
    
    public void OnEnteringExistingMainRow(int rowIndex) {
      if (MainList?.Count == 0) {
        return;
      }
      MainList?.BackupRow(rowIndex);
    }

    /// <summary>
    ///   An existing row on the parent grid has been entered.
    ///   So the main grid will be populated with the required
    ///   child entities of the entity at the specified row index.
    /// </summary>
    public void OnEnteringParentRow(int rowIndex) {
      MainList?.Populate(ParentList?.GetChildren(rowIndex));
    }

    /// <summary>
    ///   Deletes the entity at the specified row index
    ///   from the database and removes it from the list.
    /// </summary>
    public void OnRowRemoved(int rowIndex) {
      // TODO This count check should not be required
      if (MainList?.Count == 0) {
        return;
      }
      try {
        MainList?.DeleteEntity(rowIndex);
        View.OnRowUpdated();
      } catch (RowErrorException exception) {
        View.OnRowError(exception);
      }
    }

    /// <summary>
    ///   Plays the audio, if found,
    ///   of the current Piece, if any.
    /// </summary>
    /// <exception cref="ApplicationException">
    ///   The audio cannot be played.
    /// </exception>
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
    public void PlayVideo() {
      //Process.Start(GetMediumPath(Medium.Video));
    }

    /// <summary>
    ///   The error row's values will have been restored to their
    ///   originals when the change was rejected.
    ///   So put the reject new values back into the grid row.
    ///   The user can then either modify or cancel the change.
    /// </summary>
    public void RestoreRejectedValues([CanBeNull] IList<object> rejectedValues) {
      if (rejectedValues == null) {
        return;
      }
      for (var columnIndex = 0; columnIndex < Columns?.Count; columnIndex++) {
        var rejectedValue = rejectedValues[columnIndex];
        // All the rejected values will be DBNull if the user had tried to delete the row.
        if (rejectedValue != DBNull.Value) {
          View.SetCurrentRowFieldValue(Columns[columnIndex].DisplayName, rejectedValue);
        }
      } //End of for
    }

    /// <summary>
    ///   Shows the newsletter, if any, associated with the current row.
    /// </summary>
    /// <exception cref="ApplicationException">
    ///   A newsletter cannot be shown.
    /// </exception>
    public void ShowNewsletter() {
      // var newsletter = GetNewsletterToShow();
      // if (string.IsNullOrEmpty(newsletter.Path)) { } else if (!File.Exists(
      //   newsletter.Path)) {
      //   throw new ApplicationException("Newsletter file \"" + newsletter.Path
      //     + "\", specified by the Path of the "
      //     + newsletter.Date.ToString("dd MMM yyyy")
      //     + " Newsletter, cannot be found.");
      // } else {
      //   Process.Start(newsletter.Path);
      // }
    }
  }
}