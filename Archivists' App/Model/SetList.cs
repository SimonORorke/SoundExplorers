﻿using System;
using SoundExplorers.Data;

namespace SoundExplorers.Model {
  public class SetList : EntityListBase<Set> {
    protected override void AddRowsToTable() {
      foreach (var set in this) {
        Table.Rows.Add(set.Event.Location.Name, set.Event.Date, set.SetNo, set.Act?.Name,
          set.Notes);
      }
    }

    protected override EntityColumnList CreateColumns() {
      return new EntityColumnList {
        new EntityColumn(nameof(Event.Location), typeof(string),
          nameof(Set.Event),
          nameof(Event.Location)) {IsVisible = false},
        new EntityColumn(nameof(Set.Event), typeof(DateTime),
          nameof(Set.Event),
          nameof(Event.Date)) {IsVisible = false},
        new EntityColumn(nameof(Set.SetNo), typeof(int)),
        new EntityColumn(nameof(Set.Act), typeof(string),
          nameof(Set.Act), nameof(Act.Name)),
        new EntityColumn(nameof(Set.Notes), typeof(string))
      };
    }

    protected override void UpdateEntityAtRow(int rowIndex) {
      var row = Table.Rows[rowIndex];
      var newSetNo = (int)row[nameof(Set.SetNo)];
      var newActName = row[nameof(Act)].ToString();
      var newNotes = row[nameof(Set.Notes)].ToString();
      var set = this[rowIndex];
      if (newSetNo != set.SetNo) {
        set.SetNo = newSetNo;
      }
      if (newActName != set.Act?.Name) {
        set.Act = QueryHelper.Read<Act>(newActName, Session);
      }
      if (newNotes != set.Notes) {
        set.Notes = newNotes;
      }
    }
  }
}