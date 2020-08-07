﻿using System;
using System.Data.Linq;
using NUnit.Framework;
using SoundExplorersDatabase.Data;

namespace SoundExplorersDatabase.Tests.Data {
  [TestFixture]
  public class SeriesTests {
    [SetUp]
    public void Setup() {
      QueryHelper = new QueryHelper();
      DatabaseFolderPath = TestSession.CreateDatabaseFolder();
      Location1 = new Location {
        QueryHelper = QueryHelper,
        Name = Location1Name
      };
      Series1 = new Series {
        QueryHelper = QueryHelper,
        Name = Series1Name,
        Notes = Series1Notes
      };
      Series2 = new Series {
        QueryHelper = QueryHelper,
        Name = Series2Name
      };
      Event1 = new Event {
        QueryHelper = QueryHelper,
        Date = Event1Date
      };
      Event2 = new Event {
        QueryHelper = QueryHelper,
        Date = Event2Date
      };
      using (var session = new TestSession(DatabaseFolderPath)) {
        session.BeginUpdate();
        session.Persist(Location1);
        session.Persist(Series1);
        session.Persist(Series2);
        Location1.Events.Add(Event1);
        Location1.Events.Add(Event2);
        Series1.Events.Add(Event1);
        session.Persist(Event1);
        session.Persist(Event2);
        session.Commit();
      }
    }

    [TearDown]
    public void TearDown() {
      TestSession.DeleteFolderIfExists(DatabaseFolderPath);
    }

    private const string Location1Name = "Pyramid Club";
    private const string Series1Name = "Jazz Festival 2014";
    private const string Series1Notes = "My notes.";
    private const string Series2Name = "Field Recordings";

    private string DatabaseFolderPath { get; set; }
    private QueryHelper QueryHelper { get; set; }
    private Event Event1 { get; set; }
    private static DateTime Event1Date => DateTime.Today.AddDays(-1);
    private Event Event2 { get; set; }
    private static DateTime Event2Date => DateTime.Today;
    private Location Location1 { get; set; }
    private Series Series1 { get; set; }
    private Series Series2 { get; set; }

    [Test]
    public void T010_Initial() {
      using (var session = new TestSession(DatabaseFolderPath)) {
        session.BeginRead();
        Series1 = QueryHelper.Read<Series>(Series1Name, session);
        Series2 = QueryHelper.Read<Series>(Series2Name, session);
        Event1 = QueryHelper.Read<Event>(Event1.Key, session);
        session.Commit();
      }
      Assert.AreEqual(Series1Name, Series1.Name, "Series1.Name initially");
      Assert.AreEqual(Series1Notes, Series1.Notes, "Series1.Notes initially");
      Assert.AreEqual(Series2Name, Series2.Name, "Series2.Name initially");
      Assert.AreEqual(1, Series1.Events.Count,
        "Series1.Events.Count initially");
      Assert.AreSame(Series1, Event1.Series, "Event1.Series initially");
      Assert.AreEqual(Series1.Name, Event1.Series?.Name,
        "Event1.Series.Name initially");
      Assert.IsNull(Event2.Series, "Event2.Series initially");
    }

    [Test]
    public void T020_RemoveEvent() {
      using (var session = new TestSession(DatabaseFolderPath)) {
        session.BeginUpdate();
        Series1 = QueryHelper.Read<Series>(Series1Name, session);
        Event1 = QueryHelper.Read<Event>(Event1.Key, session);
        Series1.Events.Remove(Event1);
        session.Commit();
      }
      Assert.AreEqual(0, Series1.Events.Count,
        "Series1.Events.Count after remove");
      Assert.IsNull(Event1.Series, "Event1.Series after remove");
    }

    [Test]
    public void T030_DisallowDuplicate() {
      var duplicate = new Series {
        QueryHelper = QueryHelper,
        Name = Series1Name
      };
      using (var session = new TestSession(DatabaseFolderPath)) {
        session.BeginUpdate();
        Assert.Throws<DuplicateKeyException>(() =>
          session.Persist(duplicate), "Duplicate");
        session.Commit();
      }
    }
  }
}