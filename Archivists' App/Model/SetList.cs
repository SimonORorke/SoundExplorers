﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SoundExplorers.Data;

namespace SoundExplorers.Model {
  public class SetList : EntityListBase<Set, SetBindingItem> {
    [UsedImplicitly]
    public SetList() : this(true) { }

    public SetList(bool isMainList) :
      base(isMainList ? typeof(EventList) : null) { }

    private bool HasDefaultActBeenFound { get; set; }

    private void AddDefaultActIfItDoesNotExist() {
      Session.BeginUpdate();
      var defaultAct = QueryHelper.Find<Act>(
        Act.DefaultName, Session);
      if (defaultAct == null) {
        defaultAct = Act.CreateDefault();
        Session.Persist(defaultAct);
      }
      Session.Commit();
      HasDefaultActBeenFound = true;
    }

    protected override SetBindingItem CreateBindingItem(Set set) {
      return new SetBindingItem {
        Date = set.Event.Date, Location = set.Event.Location.Name!,
        SetNo = set.SetNo,
        Act = set.Act.Name,
        Genre = set.Genre.Name,
        IsPublic = set.IsPublic,
        Notes = set.Notes
      };
    }

    protected override TypedBindingList<Set, SetBindingItem> CreateBindingList(
      IList<SetBindingItem> list) {
      return new SetBindingList(list);
    }

    protected override BindingColumnList CreateColumns() {
      var result = new BindingColumnList {
        new BindingColumn(nameof(Event.Date), typeof(DateTime)) {
          IsVisible = !IsMainList
        },
        new BindingColumn(nameof(Event.Location), typeof(string)) {
          IsVisible = !IsMainList
        },
        new BindingColumn(nameof(Set.SetNo), typeof(int)) {IsInKey = true},
        new BindingColumn(nameof(Set.Act), typeof(string),
          new ReferenceType(typeof(ActList), nameof(Act.Name))),
        new BindingColumn(nameof(Set.Genre), typeof(string),
          new ReferenceType(typeof(GenreList), nameof(Genre.Name))),
        new BindingColumn(nameof(Set.IsPublic), typeof(bool)) {DisplayName = "Public?"},
        new BindingColumn(nameof(Set.Notes), typeof(string))
      };
      return result;
    }

    public override IEntityList CreateParentList() {
      return new EventList {Session = Session};
    }

    public override IdentifyingParentChildren GetIdentifyingParentChildrenForMainList(
      int rowIndex) {
      return new IdentifyingParentChildren(this[rowIndex],
        this[rowIndex].Pieces.Values.ToList());
    }

    public override void Populate(
      IdentifyingParentChildren? identifyingParentChildren = null,
      bool createBindingList = true) {
      if (!HasDefaultActBeenFound) {
        AddDefaultActIfItDoesNotExist();
      }
      base.Populate(identifyingParentChildren, createBindingList);
    }
  }
}