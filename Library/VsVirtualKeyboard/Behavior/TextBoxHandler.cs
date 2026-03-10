using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using VsVirtualKeyboard.Const;

namespace VsVirtualKeyboard.Behavior;

public class TextBoxHandler
{
    private readonly TextBox _textBox;
    private readonly DispatcherTimer _debounceTimer;

    public eVkLayout Layout { get; set; }

    public bool InternalUpdate { get; set; } = false;

    public decimal Minimum { get; set; }
    public decimal Maximum { get; set; }
    public string DecimalFormat { get; set; } = string.Empty;


    public TextBoxHandler(TextBox textBox)
    {
        _textBox = textBox;
        _debounceTimer = new DispatcherTimer(DispatcherPriority.SystemIdle, _textBox.Dispatcher)
        {
            Interval = TimeSpan.FromSeconds(1),
        };

        RegisterLoadEvents();
    }

    private void RegisterLoadEvents()
    {
        _textBox.Loaded += (_, _) =>
        {
            InternalUpdate = true;
        };

        _textBox.Unloaded += (_, _) =>
        {
            InternalUpdate = true;
            _debounceTimer.Stop();
            _debounceTimer.Tick -= NormalizeNumericInput;
        };

        _textBox.IsVisibleChanged += (_, e) =>
        {
            InternalUpdate = true;
            _debounceTimer.Stop();

            if ((bool)e.NewValue)
            {
                _debounceTimer.Tick -= NormalizeNumericInput;
                _debounceTimer.Tick += NormalizeNumericInput;
            }
            else
            {
                _debounceTimer.Tick -= NormalizeNumericInput;
            }
        };
    }

    public void RegisterLostFocusAndEnterEvents()
    {
        _textBox.LostFocus += (_, _) =>
        {
            ClampNumericValue(true);
        };

        _textBox.KeyUp += (_, e) =>
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                ClampNumericValue(true);
            }
        };
    }

    public void RemoveNumericHandler()
    {
        _textBox.PreviewTextInput -= OnPreviewTextInput;
        _textBox.TextChanged -= OnTextChanged;
        DataObject.RemovePastingHandler(_textBox, OnTextBoxPasting);
    }

    public void AddOrRemoveNumericHandler(eVkLayout layout)
    {
        Layout = layout;

        RemoveNumericHandler();

        if (Layout != eVkLayout.Text)
        {
            _textBox.TextChanged += OnTextChanged;
            _textBox.PreviewTextInput += OnPreviewTextInput;
            DataObject.AddPastingHandler(_textBox, OnTextBoxPasting);
        }
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        _debounceTimer.Stop();

        if (!InternalUpdate)
        {
            _debounceTimer.Start();
        }

        if (InternalUpdate)
        {
            InternalUpdate = false;
        }
    }

    private void NormalizeNumericInput(object? sender, EventArgs eventArgs)
    {
        var txt = _textBox.Text;
        if (Layout == eVkLayout.Numeric)
        {
            if (decimal.TryParse(txt, NumberStyles.Integer, CultureInfo.InvariantCulture, out decimal intval))
            {
                var clamped = Math.Clamp(intval, Minimum, Maximum);
                SetText(() =>
                {
                    _textBox.Text = clamped.ToString(CultureInfo.InvariantCulture);
                    _textBox.CaretIndex = _textBox.Text.Length;
                });
            }
        }
        else if (Layout == eVkLayout.Decimal)
        {
            if (decimal.TryParse(txt, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decval))
            {
                decimal clamped = Math.Clamp(decval, Minimum, Maximum);
                if (!txt.EndsWith(CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator))
                {
                    SetText(() =>
                    {
                        FormatDecimalValue(clamped);
                    });
                }
            }
        }

        _debounceTimer.Stop();
    }

    private void FormatDecimalValue(decimal decimalVal)
    {
        SetText(() =>
        {
            _textBox.Text = string.IsNullOrEmpty(DecimalFormat) ? decimalVal.ToString(CultureInfo.InvariantCulture) : decimalVal.ToString(DecimalFormat, CultureInfo.InvariantCulture);
            _textBox.CaretIndex = _textBox.Text.Length;
        });
    }

    private void SetText(Action action)
    {
        InternalUpdate = true;
        action.Invoke();
    }

    private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (Layout == eVkLayout.Decimal)
        {
            var sep = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator;

            if (e.Text == sep && !_textBox.Text.Contains(sep))
            {
                e.Handled = false;
                return;
            }
        }
        var proposedText = _textBox.Text.Insert(_textBox.CaretIndex, e.Text);
        e.Handled = !IsValidNumericInput(proposedText);
    }

    private void OnTextBoxPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(DataFormats.Text))
        {
            var text = (string)e.DataObject.GetData(DataFormats.Text);
            if (!IsValidNumericInput(text))
                e.CancelCommand();
        }
        else
            e.CancelCommand();
    }

    private bool IsValidNumericInput(string input)
    {
        if (Layout == eVkLayout.Numeric)
            return Regex.IsMatch(input, @"^-?[0-9]*$");
        else
        {
            var sep = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator;
            string esc = Regex.Escape(sep);
            return Regex.IsMatch(input, @"^-?[0-9]*(" + esc + @"[0-9]*)?$");
        }
    }

    public void ClampNumericValue(bool updateSourceBinding = false)
    {
        if (Layout == eVkLayout.Numeric)
        {
            decimal.TryParse(_textBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out decimal intval);
            var clamped = Math.Clamp(intval, Minimum, Maximum);
            SetText(() =>
            {
                _textBox.Text = clamped.ToString(CultureInfo.InvariantCulture);
                _textBox.CaretIndex = _textBox.Text.Length;
            });
        }
        else if (Layout == eVkLayout.Decimal)
        {
            decimal.TryParse(_textBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decval);
            decimal clamped = Math.Clamp(decval, Minimum, Maximum);
            if (!_textBox.Text.EndsWith(CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator))
            {
                SetText(() =>
                {
                    FormatDecimalValue(clamped);
                });
            }
        }

        if (updateSourceBinding)
        {
            _textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        }
    }

    public bool IsOuterRange()
    {
        if (Layout == eVkLayout.Numeric)
        {
            var isSuccess = decimal.TryParse(_textBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out decimal intval);
            if (!isSuccess || intval < Minimum || intval > Maximum)
            {
                return true;
            }
        }
        else if (Layout == eVkLayout.Decimal)
        {
            var isSuccess = decimal.TryParse(_textBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decval);
            if (!isSuccess || decval < Minimum || decval > Maximum)
            {
                return true;
            }
        }

        return false;
    }
}
