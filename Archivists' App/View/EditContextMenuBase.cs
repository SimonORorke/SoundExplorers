﻿using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Annotations;

namespace SoundExplorers.View {
  internal abstract class EditContextMenuBase : ContextMenuStrip {
    private CopyMenuItem? _copyMenuItem;
    private CutMenuItem? _cutMenuItem;
    private DeleteMenuItem? _deleteMenuItem;
    private DeleteSelectedRowsMenuItem? _deleteSelectedRowsMenuItem;
    private PasteMenuItem? _pasteMenuItem;
    private SelectAllMenuItem? _selectAllMenuItem;
    private SelectRowMenuItem? _selectRowMenuItem;
    private UndoMenuItem? _undoMenuItem;

    protected EditContextMenuBase() {
      // Using these custom menu items of the Edit menu on the main window menu bar 
      // caused display problems: items could become underlined or go missing.
      // That why equivalents have had to be duplicated in MainView.Designer.
      UndoMenuItem.Click += UndoMenuItem_Click;
      CutMenuItem.Click += CutMenuItem_Click;
      CopyMenuItem.Click += CopyMenuItem_Click;
      PasteMenuItem.Click += PasteMenuItem_Click;
      DeleteMenuItem.Click += DeleteMenuItem_Click;
      SelectAllMenuItem.Click += SelectAllMenuItem_Click;
      SelectRowMenuItem.Click += SelectRowMenuItem_Click;
      DeleteSelectedRowsMenuItem.Click += DeleteSelectedRowsMenuItem_Click;
      // The menu properties must be set after creating the menu items,
      // as their setters call the Items getter (implemented in derived classes),
      // which needs all the menu items to have been created.
      // With .Net 5, this was no longer sufficient,
      // as TextBoxContextMenu.OnOpening still gets invoked before this constructor
      // (in the Options dialog though not the grid).
      // The fix is to make the menu items self-instantiating properties
      // instead of instantiating them in this constructor.
      Size = new Size(61, 4);
      ShowImageMargin = false;
    }

    [NotNull] protected UndoMenuItem UndoMenuItem => _undoMenuItem ??= new UndoMenuItem();
    [NotNull] protected CutMenuItem CutMenuItem => _cutMenuItem ??= new CutMenuItem();
    [NotNull] protected CopyMenuItem CopyMenuItem => _copyMenuItem ??= new CopyMenuItem();

    [NotNull]
    protected DeleteMenuItem DeleteMenuItem => _deleteMenuItem ??= new DeleteMenuItem();

    [NotNull]
    protected PasteMenuItem PasteMenuItem => _pasteMenuItem ??= new PasteMenuItem();

    [NotNull]
    protected SelectAllMenuItem SelectAllMenuItem =>
      _selectAllMenuItem ??= new SelectAllMenuItem();

    [NotNull]
    protected SelectRowMenuItem SelectRowMenuItem =>
      _selectRowMenuItem ??= new SelectRowMenuItem();

    [NotNull]
    protected DeleteSelectedRowsMenuItem DeleteSelectedRowsMenuItem =>
      _deleteSelectedRowsMenuItem ??= new DeleteSelectedRowsMenuItem();

    protected static void DeleteTextBoxSelectedText([NotNull] TextBox textBox) {
      int selectionStart = textBox.SelectionStart;
      int count = textBox.SelectionLength;
      textBox.Text = textBox.Text.Remove(selectionStart, count);
      textBox.SelectionStart = selectionStart;
    }

    protected virtual void Undo() {
      throw new NotSupportedException();
    }

    public abstract void Cut();
    public abstract void Copy();
    public abstract void Paste();
    public abstract void Delete();
    public abstract void SelectAll();

    public virtual void SelectRow() {
      throw new NotSupportedException();
    }

    public virtual void DeleteSelectedRows() {
      throw new NotSupportedException();
    }

    private void UndoMenuItem_Click(object? sender, EventArgs e) {
      Undo();
    }

    private void CutMenuItem_Click(object? sender, EventArgs e) {
      Cut();
    }

    private void CopyMenuItem_Click(object? sender, EventArgs e) {
      Copy();
    }

    private void PasteMenuItem_Click(object? sender, EventArgs e) {
      Paste();
    }

    private void DeleteMenuItem_Click(object? sender, EventArgs e) {
      Delete();
    }

    private void SelectAllMenuItem_Click(object? sender, EventArgs e) {
      SelectAll();
    }

    private void SelectRowMenuItem_Click(object? sender, EventArgs e) {
      SelectRow();
    }

    private void DeleteSelectedRowsMenuItem_Click(object? sender, EventArgs e) {
      DeleteSelectedRows();
    }
  }
}