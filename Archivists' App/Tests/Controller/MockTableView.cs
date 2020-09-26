﻿using SoundExplorers.Controller;

namespace SoundExplorers.Tests.Controller {
  public class MockTableView : ITableView {
    public TableController Controller { get; private set; }
    public int FocusMainGridCellCount { get; private set; }
    public int FocusMainGridCellLastColumnIndex { get; private set; }
    public int FocusMainGridCellLastRowIndex { get; private set; }
    public int MakeMainGridInsertionRowCurrentCount { get; private set; }
    public int OnRowUpdatedCount { get; private set; }
    public int SelectCurrentRowOnlyCount { get; private set; }
    public int StartDatabaseUpdateErrorTimerCount { get; private set; }
    public int ShowErrorMessageCount { get; private set; }

    public void SetController(TableController controller) {
      Controller = controller;
    }

    public void FocusMainGridCell(int rowIndex, int columnIndex) {
      FocusMainGridCellCount++;
      FocusMainGridCellLastColumnIndex = columnIndex;
      FocusMainGridCellLastRowIndex = rowIndex;
    }

    public void MakeMainGridInsertionRowCurrent() {
      MakeMainGridInsertionRowCurrentCount++;
    }

    public void OnRowUpdated() {
      OnRowUpdatedCount++;
    }

    public void SelectCurrentRowOnly() {
      SelectCurrentRowOnlyCount++;
    }

    public void StartDatabaseUpdateErrorTimer() {
      StartDatabaseUpdateErrorTimerCount++;
    }

    public void ShowErrorMessage(string text) {
      ShowErrorMessageCount++;
    }
  }
}