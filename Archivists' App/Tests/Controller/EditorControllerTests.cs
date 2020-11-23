﻿using System;
using System.Data;
using JetBrains.Annotations;
using NUnit.Framework;
using SoundExplorers.Data;
using SoundExplorers.Model;
using SoundExplorers.Tests.Data;
using SoundExplorers.Tests.Model;

namespace SoundExplorers.Tests.Controller {
  [TestFixture]
  public class EditorControllerTests {
    [SetUp]
    public void Setup() {
      QueryHelper = new QueryHelper();
      Data = new TestData(QueryHelper);
      Session = new TestSession();
      View = new MockEditorView();
    }

    [TearDown]
    public void TearDown() {
      Session.DeleteDatabaseFolderIfExists();
    }

    private TestData Data { get; set; }
    private QueryHelper QueryHelper { get; set; }
    private TestSession Session { get; set; }
    private MockEditorView View { get; set; }

    [Test]
    public void AddEvents() {
      var event1Date = DateTime.Parse("2020/03/01");
      var event2Date = event1Date.AddDays(1);
      var notFoundNewsletterDate = DateTime.Parse("2345/12/31");
      const string notFoundSeriesName = "Not-Found Name";
      Session.BeginUpdate();
      try {
        Data.AddLocationsPersisted(1, Session);
        Data.AddNewslettersPersisted(2, Session);
        Data.AddSeriesPersisted(1, Session);
      } finally {
        Session.Commit();
      }
      var validLocation = Data.Locations[0];
      string validLocationName = validLocation.Name;
      var validNewsletter = Data.Newsletters[0];
      var validNewsletterDate = validNewsletter.Date;
      var validSeries = Data.Series[0];
      string validSeriesName = validSeries.Name;
      Assert.IsNotNull(validLocationName, "validLocationName");
      Assert.IsNotNull(validSeriesName, "validSeriesName");
      var controller = CreateController<Event, EventBindingItem>(typeof(EventList));
      controller.FetchData(); // Show an empty grid grid
      var editor = controller.Editor = new TestEditor<Event, EventBindingItem>(
        controller.MainBindingList);
      controller.CreateAndGoToInsertionRow();
      editor[0].Date = event1Date;
      controller.SetComboBoxCellValue(0, "Location", validLocationName);
      // Newsletter
      controller.SetComboBoxCellValue(0, "Newsletter", notFoundNewsletterDate);
      Assert.AreEqual(1, View.ShowErrorMessageCount,
        "ShowErrorMessageCount after not-found Newsletter pasted");
      Assert.AreEqual("Newsletter not found: '31 Dec 2345'",
        View.LastErrorMessage,
        "LastErrorMessage after not-found Newsletter pasted");
      controller.SetComboBoxCellValue(0, "Newsletter", validNewsletterDate);
      Assert.AreEqual(1, View.ShowErrorMessageCount,
        "ShowErrorMessageCount after valid Newsletter pasted");
      // Series
      controller.SetComboBoxCellValue(0, "Series", notFoundSeriesName);
      Assert.AreEqual(2, View.ShowErrorMessageCount,
        "ShowErrorMessageCount after not-found Series pasted");
      Assert.AreEqual("Series not found: 'Not-Found Name'", View.LastErrorMessage,
        "LastErrorMessage after not-found Series pasted");
      controller.SetComboBoxCellValue(0, "Series", validSeriesName);
      Assert.AreEqual(2, View.ShowErrorMessageCount,
        "ShowErrorMessageCount after valid Series pasted");
      controller.OnMainGridRowValidated(0);
      Assert.AreEqual(1, validLocation.Events.Count, "Events.Count after 1st add");
      var event1 = validLocation.Events[0];
      Assert.AreSame(validLocation, event1.Location, "event1.Location");
      Assert.AreSame(validNewsletter, event1.Newsletter, "event1.Newsletter");
      Assert.AreSame(validSeries, event1.Series, "event1.Series");
      controller.CreateAndGoToInsertionRow();
      editor[1].Date = event2Date;
      controller.SetComboBoxCellValue(1, "Location", validLocationName);
      // Test that the user can reset the newsletter date to the special date
      // that indicated that the event's newsletter is to be unassigned.
      controller.SetComboBoxCellValue(1, "Newsletter", EntityBase.InitialDate);
      controller.OnMainGridRowValidated(1);
      Assert.AreEqual(2, validLocation.Events.Count, "Events.Count after 2nd add");
      var event2 = validLocation.Events[1];
      Assert.IsNull(event2.Newsletter, "event2.Newsletter");
    }

    [Test]
    public void Edit() {
      const string name1 = "Auntie";
      const string name2 = "Uncle";
      var controller = 
        CreateController<Location, NotablyNamedBindingItem<Location>>(
          typeof(LocationList));
      var editor = controller.Editor = 
        new TestEditor<Location, NotablyNamedBindingItem<Location>>();
      Assert.IsFalse(controller.IsParentTableToBeShown, "IsParentTableToBeShown");
      controller.FetchData(); // The grid will be empty initially
      Assert.AreEqual("Location", controller.MainTableName, "MainTableName");
      editor.SetBindingList(controller.MainBindingList);
      controller.CreateAndGoToInsertionRow();
      editor[0].Name = name1;
      editor[0].Notes = "Disestablishmentarianism";
      controller.OnMainGridRowValidated(0);
      controller.CreateAndGoToInsertionRow();
      editor[1].Name = name2;
      editor[1].Notes = "Bob";
      controller.OnMainGridRowValidated(1);
      Assert.AreEqual(2, View.OnRowAddedOrDeletedCount, "OnRowAddedOrDeletedCount");
      controller.FetchData(); // Refresh grid
      editor.SetBindingList(controller.MainBindingList);
      Assert.AreEqual(2, editor.Count, "editor.Count after FetchData #2");
      controller.OnMainGridRowEnter(1);
      // Disallow rename to duplicate
      var exception = Assert.Catch<DatabaseUpdateErrorException>(
        () => editor[1].Name = name1,
        "Rename name should have thrown DatabaseUpdateErrorException.");
      Assert.AreEqual(name1, editor[1].Name,
        "Still duplicate name before error message shown for duplicate rename");
      controller.OnExistingRowCellUpdateError(1, "Name", exception);
      Assert.AreEqual(1, View.EditMainGridCurrentCellCount,
        "EditMainGridCurrentCellCount after error message shown for duplicate rename");
      Assert.AreEqual(1, View.FocusMainGridCellCount,
        "FocusMainGridCellCount after error message shown for duplicate rename");
      Assert.AreEqual(0, View.FocusMainGridCellColumnIndex,
        "FocusMainGridCellColumnIndex after error message shown for duplicate rename");
      Assert.AreEqual(1, View.FocusMainGridCellRowIndex,
        "FocusMainGridCellRowIndex after error message shown for duplicate rename");
      Assert.AreEqual(name2, editor[1].Name,
        "Original name restored after error message shown for duplicate rename");
      Assert.AreEqual(1, View.RestoreMainGridCurrentRowCellErrorValueCount,
        "RestoreMainGridCurrentRowCellErrorValueCount after error message shown for duplicate rename");
      Assert.AreEqual(1, View.ShowErrorMessageCount,
        "ShowErrorMessageCount after error message shown for duplicate rename");
      // For unknown reason, the the error handling is set up,
      // this event gets raise twice if there's a cell edit error,
      // as in the case of this rename,
      // the second time with a null exception.
      // So check that this is allowed for and has no effect.
      controller.OnExistingRowCellUpdateError(1, "Name", null);
      Assert.AreEqual(1, View.ShowErrorMessageCount,
        "ShowErrorMessageCount after null error");
      // Check that an exception of an unsupported type is rethrown
      Assert.Throws<InvalidOperationException>(
        () => controller.OnExistingRowCellUpdateError(1, "Name", new InvalidOperationException()),
        "Unsupported exception type");
      // Disallow insert with duplicate name
      controller.CreateAndGoToInsertionRow();
      editor[2].Name = name1;
      Assert.AreEqual(2, View.OnRowAddedOrDeletedCount,
        "OnRowAddedOrDeletedCount unchanged after duplicate insert");
      Assert.AreEqual(3, editor.Count,
        "editor.Count before error message shown for duplicate insert");
      controller.OnMainGridRowValidated(2);
      Assert.AreEqual(2, View.ShowErrorMessageCount,
        "ShowErrorMessageCount after error message shown for duplicate insert");
      Assert.AreEqual(1, View.MakeMainGridRowCurrentCount,
        "MakeMainGridRowCurrentCount after error message shown for duplicate insert");
      // When the insertion error message was shown,
      // focus was forced back to the error row,
      // now no longer the insertion row,
      // in EditorController.ShowError.
      // That would raise the EditorView.MainGridOnRowEnter event,
      // which we have to simulate here.
      controller.OnMainGridRowEnter(2);
      // Then the user opted to cancel out of adding the new row
      // rather than fixing it so that the add would work.
      // That would raise the EditorView.MainRowValidated event, 
      // even though nothing has changed.
      // We have to simulate that here to test to test
      // that the unwanted new row would get removed from the grid
      // (if there was a real grid).
      controller.OnMainGridRowValidated(2);
      Assert.AreEqual(3, View.OnRowAddedOrDeletedCount,
        "OnRowAddedOrDeletedCount after cancel from insert error row");
      Assert.AreEqual(2, editor.Count,
        "editor.Count after cancel from insert error row");
      // Delete the second item
      controller.OnMainGridRowEnter(1);
      controller.OnMainGridRowRemoved(1);
      Assert.AreEqual(4, View.OnRowAddedOrDeletedCount,
        "OnRowAddedOrDeletedCount after delete");
      controller.FetchData(); // Refresh grid
      editor.SetBindingList(controller.MainBindingList);
      Assert.AreEqual(1, editor.Count, "editor.Count after FetchData #3");
    }

    [Test]
    public void ErrorOnDelete() {
      Session.BeginUpdate();
      try {
        Data.AddEventTypesPersisted(1, Session);
        Data.AddLocationsPersisted(2, Session);
        Data.AddEventsPersisted(3, Session, Data.Locations[1]);
      } finally {
        Session.Commit();
      }
      // The second Location cannot be deleted because it is a parent of 3 child Events.
      var controller = CreateController<Location, NotablyNamedBindingItem<Location>>(
        typeof(LocationList));
      var editor = controller.Editor = 
        new TestEditor<Location, NotablyNamedBindingItem<Location>>();
      controller.FetchData(); // Populate grid
      editor.SetBindingList(controller.MainBindingList);
      controller.CreateAndGoToInsertionRow();
      controller.OnMainGridRowRemoved(1);
      Assert.AreEqual(1, View.ShowErrorMessageCount,
        "ShowErrorMessageCount after error message shown for disallowed delete");
      Assert.AreEqual(1, View.SelectCurrentRowOnlyCount,
        "SelectCurrentRowOnlyCount after error message shown for disallowed delete");
      controller.OnMainGridRowEnter(1);
      controller.TestUnsupportedLastChangeAction = true;
      Assert.Throws<NotSupportedException>(() => controller.OnMainGridRowRemoved(1),
        "Unsupported last change action");
    }

    [Test]
    public void ExistingEventValidatePastedParents() {
      Session.BeginUpdate();
      try {
        Data.AddEventTypesPersisted(1, Session);
        Data.AddLocationsPersisted(1, Session);
        Data.AddNewslettersPersisted(1, Session);
        Data.AddSeriesPersisted(1, Session);
        Data.AddEventsPersisted(1, Session);
      } finally {
        Session.Commit();
      }
      var controller = CreateController<Event, EventBindingItem>(typeof(EventList));
      controller.FetchData(); // Populate grid
      Assert.IsTrue(controller.DoesColumnReferenceAnotherEntity("Newsletter"),
        "Newsletter DoesColumnReferenceAnotherEntity");
      var editor = controller.Editor = new TestEditor<Event, EventBindingItem>(
        controller.MainBindingList);
      // Newsletter
      var selectedNewsletter = Data.Newsletters[0];
      var selectedNewsletterDate = selectedNewsletter.Date;
      controller.OnMainGridRowEnter(0);
      controller.SetComboBoxCellValue(0, "Newsletter", selectedNewsletterDate);
      Assert.AreEqual(0, View.ShowErrorMessageCount,
        "ShowErrorMessageCount after valid Newsletter selection");
      Assert.AreEqual(selectedNewsletterDate, editor[0].Newsletter,
        "Newsletter in editor after valid Newsletter selection");
      Assert.AreSame(selectedNewsletter, ((Event)controller.GetMainList()[0]).Newsletter,
        "Newsletter entity after valid Newsletter selection");
      var notFoundDate = DateTime.Parse("2345/12/31");
      controller.OnMainGridRowEnter(0);
      var exception = Assert.Catch(
        ()=>controller.SetComboBoxCellValue(
          0, "Newsletter", notFoundDate));
      Assert.IsInstanceOf<RowNotInTableException>(exception, 
        "Exception on not-found Newsletter pasted");
      Assert.AreEqual(0, View.ShowErrorMessageCount,
        "ShowErrorMessageCount after not-found Newsletter pasted but before OnExistingRowCellUpdateError");
      controller.OnExistingRowCellUpdateError(0, "Newsletter", exception);
      Assert.AreEqual(1, View.ShowErrorMessageCount,
        "ShowErrorMessageCount after not-found Newsletter pasted and after OnExistingRowCellUpdateError");
      Assert.AreEqual("Newsletter not found: '31 Dec 2345'",
        View.LastErrorMessage,
        "LastErrorMessage after not-found Newsletter pasted");
      Assert.AreEqual(selectedNewsletterDate, editor[0].Newsletter,
        "Newsletter in editor after not-found Newsletter pasted");
      Assert.AreSame(selectedNewsletter, ((Event)controller.GetMainList()[0]).Newsletter,
        "Newsletter entity after not-found Newsletter pasted");
      // Series
      var selectedSeries = Data.Series[0];
      string selectedSeriesName = selectedSeries.Name;
      Assert.IsNotNull(selectedSeriesName, "selectedSeriesName");
      controller.OnMainGridRowEnter(0);
      controller.SetComboBoxCellValue(0, "Series", selectedSeriesName);
      Assert.AreEqual(1, View.ShowErrorMessageCount,
        "ShowErrorMessageCount after valid Series selection");
      Assert.AreEqual(selectedSeriesName, editor[0].Series,
        "Series in editor after valid Series selection");
      Assert.AreSame(selectedSeries, ((Event)controller.GetMainList()[0]).Series,
        "Series entity after valid Series selection");
      const string notFoundName = "Not-Found Name";
      controller.OnMainGridRowEnter(0);
      exception = Assert.Catch(
        ()=>controller.SetComboBoxCellValue(
          0, "Series", notFoundName));
      Assert.IsInstanceOf<RowNotInTableException>(exception, 
        "Exception on not-found Series pasted");
      Assert.AreEqual(1, View.ShowErrorMessageCount,
        "ShowErrorMessageCount after not-found Series pasted but before OnExistingRowCellUpdateError");
      controller.OnExistingRowCellUpdateError(0, "Series", exception);
      Assert.AreEqual(2, View.ShowErrorMessageCount,
        "ShowErrorMessageCount after not-found Series pasted and after OnExistingRowCellUpdateError");
      Assert.AreEqual("Series not found: 'Not-Found Name'", View.LastErrorMessage,
        "LastErrorMessage after not-found Series pasted");
      Assert.AreEqual(selectedSeriesName, editor[0].Series,
        "Series in editor after not-found Series pasted");
      Assert.AreSame(selectedSeries, ((Event)controller.GetMainList()[0]).Series,
        "Series entity after not-found Series pasted");
    }

    [Test]
    public void FormatExceptionOnInsert() {
      Session.BeginUpdate();
      try {
        Data.AddNewslettersPersisted(3, Session);
      } finally {
        Session.Commit();
      }
      var controller = CreateController<Newsletter, NewsletterBindingItem>(
        typeof(NewsletterList));
      controller.FetchData(); // Populate grid
      controller.Editor = new TestEditor<Newsletter, NewsletterBindingItem>(
        controller.MainBindingList);
      controller.CreateAndGoToInsertionRow();
      var exception = new FormatException("Potato is not a valid DateTime.");
      controller.OnExistingRowCellUpdateError(3, "Date", exception);
      Assert.AreEqual(1, View.ShowErrorMessageCount, "ShowErrorMessageCount");
    }

    [Test]
    public void FormatExceptionOnUpdate() {
      Session.BeginUpdate();
      try {
        Data.AddEventTypesPersisted(2, Session);
        Data.AddLocationsPersisted(2, Session);
        Data.AddEventsPersisted(3, Session);
        Data.AddNewslettersPersisted(1, Session);
        Data.AddSeriesPersisted(1, Session);
      } finally {
        Session.Commit();
      }
      var controller = CreateController<Event, EventBindingItem>(typeof(EventList));
      controller.FetchData(); // Populate grid
      var editor = controller.Editor = new TestEditor<Event, EventBindingItem>(
        controller.MainBindingList);
      controller.OnMainGridRowEnter(2);
      string changedEventType = Data.EventTypes[1].Name;
      string changedLocation = Data.Locations[1].Name;
      var changedNewsletter = Data.Newsletters[0].Date;
      const string changedNotes = "Changed notes";
      string changedSeries = Data.Series[0].Name;
      editor[2].EventType = changedEventType;
      editor[2].Location = changedLocation;
      editor[2].Newsletter = changedNewsletter;
      editor[2].Notes = changedNotes;
      editor[2].Series = changedSeries;
      // Simulate pasting text into the Date cell.
      var exception = new FormatException("Potato is not a valid value for DateTime.");
      controller.OnExistingRowCellUpdateError(2, "Date", exception);
      Assert.AreEqual(1, View.ShowErrorMessageCount, "ShowErrorMessageCount");
      Assert.AreEqual(changedEventType, editor[2].EventType, "EventType");
      Assert.AreEqual(changedLocation, editor[2].Location, "Location");
      Assert.AreEqual(changedNewsletter, editor[2].Newsletter, "Newsletter");
      Assert.AreEqual(changedNotes, editor[2].Notes, "Notes");
      Assert.AreEqual(changedSeries, editor[2].Series, "Series");
    }

    [Test]
    public void GetColumnDisplayName() {
      Session.BeginUpdate();
      Data.AddNewslettersPersisted(1, Session);
      Session.Commit();
      var controller = CreateController<Newsletter, NewsletterBindingItem>(
        typeof(NewsletterList));
      controller.FetchData(); // Populate grid
      Assert.AreEqual("Date", controller.GetColumnDisplayName("Date"), "Date");
      Assert.AreEqual("URL", controller.GetColumnDisplayName("Url"), "Url");
    }

    [Test]
    public void GridSplitterDistance() {
      const int distance = 123;
      var controller = CreateController<Event, EventBindingItem>(typeof(EventList));
      controller.GridSplitterDistance = distance;
      Assert.AreEqual(distance, controller.GridSplitterDistance);
    }

    [Test]
    public void OnParentGridRowEntered() {
      Session.BeginUpdate();
      try {
        Data.AddEventTypesPersisted(1, Session);
        Data.AddGenresPersisted(1, Session);
        Data.AddLocationsPersisted(1, Session);
        Data.AddEventsPersisted(2, Session);
        Data.AddSetsPersisted(3, Session, Data.Events[0]);
        Data.AddSetsPersisted(5, Session, Data.Events[1]);
      } finally {
        Session.Commit();
      }
      var controller = CreateController<Set, SetBindingItem>(typeof(SetList));
      controller.FetchData(); // Populate grid
      Assert.AreEqual(2, controller.ParentBindingList?.Count, "Parent list count");
      Assert.AreEqual(3, controller.MainBindingList?.Count, "Main list count initially");
      controller.OnParentGridRowEntered(1);
      Assert.AreEqual(5, controller.MainBindingList?.Count,
        "Main list count when 2nd parent selected");
    }

    [Test]
    public void ShowWarningMessage() {
      var controller = CreateController<Event, EventBindingItem>(typeof(EventList));
      controller.ShowWarningMessage("Warning! Warning!");
      Assert.AreEqual(1, View.ShowWarningMessageCount);
    }

    [NotNull]
    private TestEditorController<TEntity, TBindingItem> 
      CreateController<TEntity, TBindingItem>([NotNull] Type mainListType) 
      where TEntity : EntityBase, new()
      where TBindingItem : BindingItemBase<TEntity, TBindingItem>, new() {
      return new TestEditorController<TEntity, TBindingItem>(
        View, mainListType, QueryHelper, Session);
    }
  }
}