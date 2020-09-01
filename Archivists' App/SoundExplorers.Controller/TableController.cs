using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using SoundExplorers.Common;
using SoundExplorers.Data;

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
    ///   The view to be shown.
    /// </param>
    /// <param name="tableName">
    ///   The name of the table whose data is to be displayed.
    /// </param>
    public TableController([NotNull] ITableView view,
      [NotNull] string tableName) {
      View = view;
      View.SetController(this);
      TableName = tableName;
    }

    /// <summary>
    ///   Gets metadata about the database columns
    ///   represented by the Entity's field properties.
    /// </summary>
    [CanBeNull]
    public IEntityColumnList Columns => Entities?.Columns;

    /// <summary>
    ///   Gets the data set containing the main table
    ///   and, if specified, the parent table.
    /// </summary>
    [CanBeNull]
    public DataSet DataSet => Entities?.DataSet;

    /// <summary>
    ///   The entity list representing the table whose data is shown in the table editor.
    /// </summary>
    [CanBeNull]
    private IEntityList Entities { get; set; }

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
        new Option($"{TableName}.GridSplitterDistance"));

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
        new Option($"{TableName}.ImageSplitterDistance"));

    public bool IsParentTableToBeShown => ParentTableName != null;

    /// <summary>
    ///   Gets the list of entities representing the main table's
    ///   parent table, if specified.
    /// </summary>
    [CanBeNull]
    public IEntityList ParentList => Entities?.ParentList;

    [CanBeNull] public string ParentTableName => ParentList?.TableName;
    [CanBeNull] public DataTable Table => Entities?.Table;
    [NotNull] public string TableName { get; }
    [NotNull] private ITableView View { get; }

    /// <summary>
    ///   If the main grid represents Pieces or Credits,
    ///   we need to conserve the current state of the
    ///   Piece and its credits.
    ///   This information will be required when
    ///   saving any changes to the current Piece or Credit
    ///   to the metadata tags of the Piece's audio file.
    /// </summary>
    public void ConservePieceIfRequired(int rowIndex) {
      if (Entities is CreditList) {
        var parentPiece = GetPiece();
        if (parentPiece != null) {
          var credits = (
            from Credit credit in (CreditList)Entities
            where credit.Date == parentPiece.Date
                  && credit.Location == parentPiece.Location
                  && credit.Set == parentPiece.Set
                  && credit.Piece == parentPiece.PieceNo
            select credit).ToList();
          parentPiece.Credits = new CreditList(true);
          foreach (var credit in credits) {
            parentPiece.Credits.Add(credit);
          }
          parentPiece.Original = parentPiece.Clone();
        }
        // } else if (Entities is ImageList) {
        //   var image = (Image)Entities[e.RowIndex];
        //   ShowImageOrMessage(image.Path);
      } else if (Entities is PieceList pieceList) {
        var piece = (
          from Piece p in pieceList
          where p.Date == (DateTime)View.GetFieldValue(nameof(p.Date), rowIndex)
                && p.Location == View.GetFieldValue(nameof(p.Location), rowIndex)
                  .ToString()
                && p.Set == (int)View.GetFieldValue(nameof(p.Set), rowIndex)
                && p.PieceNo == (int)View.GetFieldValue(nameof(p.PieceNo), rowIndex)
          select p).FirstOrDefault();
        if (piece != null) {
          piece.Credits = piece.FetchCredits();
          piece.Original = piece.Clone();
        }
      }
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
      string path = GetMediumPath(Medium.Audio);
      var dummy = new AudioFile(path);
    }

    private void Entities_RowError(object sender, RowErrorEventArgs e) {
      View.OnRowError(e);
    }

    private void Entities_RowUpdated(object sender, RowUpdatedEventArgs e) {
      string databaseUpdateMessage = e.Message;
      string mediaTagsUpdateErrorMessage = null;
      if (e.Entity is IMediaEntity mediaEntity) {
        try {
          string updateTagsMessage = mediaEntity.UpdateTags();
          if (!string.IsNullOrEmpty(updateTagsMessage)) {
            databaseUpdateMessage += ". " + updateTagsMessage;
          }
        } catch (ApplicationException ex) {
          mediaTagsUpdateErrorMessage = ex.Message;
        }
      }
      View.OnRowUpdated(databaseUpdateMessage, mediaTagsUpdateErrorMessage);
    }

    public void FetchData() {
      Entities = TableName == nameof(ArtistInImage)
        ? new ArtistInImageList()
        : Factory<IEntityList>.Create(TableName);
      Entities.RowError += Entities_RowError;
      Entities.RowUpdated += Entities_RowUpdated;
    }

    /// <summary>
    ///   Returns the path of the specified medium file of the current Piece
    ///   if the file exists.
    /// </summary>
    /// <param name="medium">
    ///   The required medium.
    /// </param>
    /// <exception cref="ApplicationException">
    ///   A suitable Piece is not selected
    ///   or the file is not specified or does not exist.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   The specified medium is not supported.
    /// </exception>
    private string GetMediumPath(Medium medium) {
      var piece = GetPiece();
      string indefiniteArticle;
      string path;
      switch (medium) {
        case Medium.Audio:
          indefiniteArticle = "An";
          path = piece.AudioPath;
          break;
        case Medium.Video:
          indefiniteArticle = "A";
          path = piece.VideoPath;
          break;
        default:
          throw new ArgumentOutOfRangeException(
            nameof(medium),
            medium,
            "Medium " + medium + " is not supported.");
      } //End of switch (medium)
      if (string.IsNullOrEmpty(path)) {
        throw new ApplicationException(
          indefiniteArticle + " " + medium.ToString().ToLower()
          + " file has not been specified for the Piece.");
      }
      if (!File.Exists(path)) {
        throw new ApplicationException(
          medium + " file \"" + path + "\" cannot be found.");
      }
      return path;
      //Process.Start(path);
    }

    /// <summary>
    ///   Returns the Newsletter to be shown.
    /// </summary>
    /// <returns>
    ///   The Newsletter to be shown.
    /// </returns>
    /// <exception cref="ApplicationException">
    ///   A suitable Newsletter to show is not associated with the selected row.
    /// </exception>
    private Newsletter GetNewsletterToShow() {
      // if (Entities is ArtistInImageList) {
      //   if (Controller.ParentList.Count > 0) {
      //     var image =
      //       (Image)Controller.ParentList[ParentCurrentRow.Index];
      //     return image.FetchNewsletter();
      //   }
      //   throw new ApplicationException(
      //     "You cannot show the newsletter for an Image's Performance because "
      //     + "no Images are listed in the ArtistInImageList window.");
      // }
      if (Entities is CreditList) {
        if (ParentList?.Count > 0) {
          var piece =
            (Piece)ParentList[View.ParentCurrentIndex];
          return piece.FetchNewsletter();
        }
        throw new ApplicationException(
          "You cannot show a Piece's newsletter because "
          + "no Pieces are listed in the Credit window.");
      }
      // if (Entities is ImageList) {
      //   if (MainCurrentRow.IsNewRow) {
      //     throw new ApplicationException(
      //       "You must add the new Image before you can show its Performance's newsletter.");
      //   }
      //   if (MainGrid.IsCurrentCellInEditMode) {
      //     throw new ApplicationException(
      //       "While you are editing the Image, "
      //       + "you cannot show its Performance's newsletter.");
      //   }
      //   var image =
      //     (Image)Entities[MainCurrentRow.Index];
      //   if (image.Location != MainCurrentRow.Cells["Location"].Value.ToString()
      //       || image.Date != (DateTime)MainCurrentRow.Cells["Date"].Value) {
      //     throw new ApplicationException(
      //       "You must save or cancel changes to the Image "
      //       + "before you can show its Performance's newsletter.");
      //   }
      //   return image.FetchNewsletter();
      // }
      if (Entities is NewsletterList) {
        if (!View.IsThereACurrentMainEntity) {
          throw new ApplicationException(
            "You must add the new Newsletter before you can show it.");
        }
        if (View.IsEditing) {
          throw new ApplicationException(
            "You cannot show the Newsletter while you are editing it.");
        }
        var newsletter = (Newsletter)Entities[View.MainCurrentIndex];
        if (newsletter.Path !=
            View.GetCurrentFieldValue(nameof(newsletter.Path)).ToString()) {
          throw new ApplicationException(
            "You must save or cancel changes to the Newsletter before you can show it.");
        }
        return newsletter;
      }
      if (Entities is PieceList) {
        if (ParentList?.Count > 0) {
          var set =
            (Set)ParentList[View.ParentCurrentIndex];
          return set.FetchNewsletter();
        }
        throw new ApplicationException(
          "You cannot show a Set's newsletter because "
          + "no Sets are listed in the Piece window.");
      }
      if (Entities is PerformanceList) {
        if (!View.IsThereACurrentMainEntity) {
          throw new ApplicationException(
            "You must add the new Performance before you can show its newsletter.");
        }
        if (View.IsEditing) {
          throw new ApplicationException(
            "You cannot show the Performance's newsletter "
            + "while you are editing the Performance.");
        }
        var performance = (Performance)Entities[View.MainCurrentIndex];
        if (performance.Newsletter !=
            (DateTime)View.GetCurrentFieldValue(nameof(performance.Newsletter))) {
          throw new ApplicationException(
            "You must save or cancel changes to the Performance "
            + "before you can show its newsletter.");
        }
        return performance.FetchNewsletter();
      }
      if (Entities is SetList) {
        if (ParentList?.Count > 0) {
          var performance =
            (Performance)ParentList[View.ParentCurrentIndex];
          return performance.FetchNewsletter();
        }
        throw new ApplicationException(
          "You cannot show a Performance's newsletter because "
          + "no Performances are listed in the Set window.");
      }
      throw new ApplicationException(
        "Newsletters are not associated with the " + TableName + " table."
        + Environment.NewLine
        + "To show a newsletter, first select a row in a "
        + "Credit, Newsletter, "
        + "Performance, Piece or Set table window.");
    }

    /// <summary>
    ///   Returns the Piece to be played.
    /// </summary>
    /// <returns>
    ///   The Piece to be played.
    /// </returns>
    /// <exception cref="ApplicationException">
    ///   A suitable Piece to play is not selected.
    /// </exception>
    private Piece GetPiece() {
      switch (Entities) {
        case CreditList _ when ParentList?.Count > 0:
          return (Piece)ParentList[View.ParentCurrentIndex];
        case CreditList _:
          throw new ApplicationException(
            "You cannot play a Piece because no Pieces are listed in the Credit window.");
        case PieceList dummy when !View.IsThereACurrentMainEntity:
          throw new ApplicationException(
            "You must add the new Piece before you can play it.");
        case PieceList dummy when View.IsEditing:
          throw new ApplicationException(
            "You cannot play the Piece while you are editing it.");
        // Because this is the detail grid in a master-detail relationship
        // the index of the Piece in the grid is probably different
        // from its index in the entity list,
        // which will contain all the Pieces of all the Performances in the parent grid.
        // So we need to find it the hard way.
        case PieceList pieceList: {
          var piece = (
            from Piece p in pieceList
            where p.Date == (DateTime)View.GetCurrentFieldValue(nameof(p.Date))
                  && p.Location ==
                  View.GetCurrentFieldValue(nameof(p.Location)).ToString()
                  && p.Set == (int)View.GetCurrentFieldValue(nameof(p.Set))
                  && p.PieceNo == (int)View.GetCurrentFieldValue(nameof(p.PieceNo))
            select p).FirstOrDefault();
          if (piece == null) {
            // Piece not found.
            // As Date, Location and Set are invisible
            // (because common to the parent Set row),
            // PieceNo, AudioPath or VideoPath must have been 
            // changed without yet updating the database.
            throw new ApplicationException(
              "You must save or cancel changes to the Piece before you can play it.");
          }
          return piece;
        }
        default:
          throw new ApplicationException(
            "Media files are not associated with the " + TableName + " table."
            + Environment.NewLine
            + "To play a piece, first select a row in a Credit or Piece table window.");
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
      Process.Start(GetMediumPath(Medium.Audio));
    }

    /// <summary>
    ///   Plays the video, if found,
    ///   of the current Piece, if any.
    /// </summary>
    /// <exception cref="ApplicationException">
    ///   The video cannot be played.
    /// </exception>
    public void PlayVideo() {
      Process.Start(GetMediumPath(Medium.Video));
    }

    public void SetDefaultFolder([NotNull] string columnName, [NotNull] FileInfo file) {
      switch (columnName) {
        case "AudioPath": // Piece.AudioPath
          Piece.DefaultAudioFolder = file.Directory;
          break;
        case "Path":
          // if (Entities is ImageList) { // Image.Path
          //   Image.DefaultFolder = file.Directory;
          // } else if (Entities is NewsletterList) { // Newsletter.Path
          if (Entities is NewsletterList) { // Newsletter.Path
            Newsletter.DefaultFolder = file.Directory;
          } else {
            throw new NotSupportedException(
              TableName + ".Path is not supported.");
          }
          break;
        case "VideoPath": // Piece.VideoPath
          Piece.DefaultVideoFolder = file.Directory;
          break;
      } //End of switch
    }

    /// <summary>
    ///   Shows the newsletter, if any, associated with the current row.
    /// </summary>
    /// <exception cref="ApplicationException">
    ///   A newsletter cannot be shown.
    /// </exception>
    public void ShowNewsletter() {
      var newsletter = GetNewsletterToShow();
      if (string.IsNullOrEmpty(newsletter.Path)) { } else if (!File.Exists(
        newsletter.Path)) {
        throw new ApplicationException("Newsletter file \"" + newsletter.Path
          + "\", specified by the Path of the "
          + newsletter.Date.ToString("dd MMM yyyy")
          + " Newsletter, cannot be found.");
      } else {
        Process.Start(newsletter.Path);
      }
    }

    /// <summary>
    ///   Updates the database table with any changes that have been input
    ///   and refreshes the list of Entities.
    /// </summary>
    public void Update(Dictionary<string, object> oldKeyFields = null) {
      Entities?.Update(oldKeyFields);
    }

    private enum Medium {
      Audio,
      Video
    }
  }
}