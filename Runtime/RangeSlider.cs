using UnityEngine;
using UnityEngine.UIElements;

namespace NB.UIElements
{
    public class RangeSlider : BaseField<Vector2>
    {
        public enum InteractionMode
        {
            /// <summary>
            /// Interaction is completely disabled, the slider cannot be manipulated by the user.
            /// </summary>
            Disabled,

            /// <summary>
            /// Only the thumbs/handles can be moved.
            /// </summary>
            ThumbsOnly,

            /// <summary>
            /// Only the selected region can be dragged, the thumbs cannot be used to resize it.
            /// </summary>
            SelectionOnly,

            /// <summary>
            /// The thumbs/handles and the selected region can all be dragged to adjust the slider values.
            /// </summary>
            ThumbsAndSelection
        }

        public new static readonly string ussClassName = "nb-range-slider";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";
        public static readonly string trackUssClassName = ussClassName + "__track";
        public static readonly string fillUssClassName = ussClassName + "__fill";
        public static readonly string markerContainerUssClassName = ussClassName + "__marker-container";
        public static readonly string singleMarkerUssClassName = ussClassName + "__single-marker";
        public static readonly string rangeMarkerUssClassName = ussClassName + "__range-marker";
        public static readonly string handleUssClassName = ussClassName + "__handle";
        public static readonly string lowerHandleUssClassName = ussClassName + "__lower-handle";
        public static readonly string upperHandleUssClassName = ussClassName + "__upper-handle";

        public RangeSlider() : this(null, 0, 1, 0, 1)
        {
        }

        public RangeSlider(float minValue, float maxValue, float minLimit, float maxLimit) : this(null, minValue, maxValue, minLimit, maxLimit)
        {
        }

        public RangeSlider(string label, float minValue, float maxValue, float minLimit, float maxLimit) : base(label, null)
        {
            HighLimit = maxLimit;
            LowLimit = minLimit;
            MaxValue = maxValue;
            MinValue = minValue;

            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);

            pickingMode = PickingMode.Ignore;

            m_visualInput = this.Q(null, "unity-base-field__input");
            m_visualInput.pickingMode = PickingMode.Position;
            m_visualInput.AddToClassList(inputUssClassName);

            m_track = new VisualElement() { name = "range-slider-track" };
            m_track.AddToClassList(trackUssClassName);
            m_visualInput.Add(m_track);

            m_fill = new VisualElement() { name = "range-slider-fill" };
            m_fill.AddToClassList(fillUssClassName);
            m_track.Add(m_fill);

            m_markerContainer = new VisualElement() { name = "range-slider-markers" };
            m_markerContainer.AddToClassList(markerContainerUssClassName);
            m_markerContainer.pickingMode = PickingMode.Ignore;
            m_track.Add(m_markerContainer);

            m_lowHandle = new VisualElement() { name = "range-slider-handle-low" };
            m_lowHandle.AddToClassList(handleUssClassName);
            m_lowHandle.AddToClassList(lowerHandleUssClassName);
            m_fill.Add(m_lowHandle);
            m_lowHandle.RegisterCallback<PointerDownEvent>((evt) =>
            {
                if (Mode == InteractionMode.ThumbsOnly || Mode == InteractionMode.ThumbsAndSelection)
                    PointerCaptureHelper.CapturePointer(m_lowHandle, evt.pointerId);
            }, TrickleDown.TrickleDown);
            m_lowHandle.RegisterCallback<PointerUpEvent>((evt) =>
            {
                PointerCaptureHelper.ReleasePointer(m_lowHandle, evt.pointerId);
            }, TrickleDown.TrickleDown);
            m_lowHandle.RegisterCallback<PointerMoveEvent>((evt) =>
            {
                if ((Mode == InteractionMode.ThumbsOnly || Mode == InteractionMode.ThumbsAndSelection) &&
                    PointerCaptureHelper.HasPointerCapture(m_lowHandle, evt.pointerId))
                {
                    MinValue = ((HighLimit - LowLimit) * m_track.WorldToLocal(evt.position).x / m_track.resolvedStyle.width) + LowLimit;
                }
            }, TrickleDown.TrickleDown);

            m_highHandle = new VisualElement() { name = "range-slider-handle-high" };
            m_highHandle.AddToClassList(handleUssClassName);
            m_highHandle.AddToClassList(upperHandleUssClassName);
            m_fill.Add(m_highHandle);
            m_highHandle.RegisterCallback<PointerDownEvent>((evt) =>
            {
                if (Mode == InteractionMode.ThumbsOnly || Mode == InteractionMode.ThumbsAndSelection)
                    PointerCaptureHelper.CapturePointer(m_highHandle, evt.pointerId);
            }, TrickleDown.TrickleDown);
            m_highHandle.RegisterCallback<PointerUpEvent>((evt) =>
            {
                PointerCaptureHelper.ReleasePointer(m_highHandle, evt.pointerId);
            }, TrickleDown.TrickleDown);
            m_highHandle.RegisterCallback<PointerMoveEvent>((evt) =>
            {
                if ((Mode == InteractionMode.ThumbsOnly || Mode == InteractionMode.ThumbsAndSelection) &&
                    PointerCaptureHelper.HasPointerCapture(m_highHandle, evt.pointerId))
                {
                    MaxValue = ((HighLimit - LowLimit) * m_track.WorldToLocal(evt.position).x / m_track.resolvedStyle.width) + LowLimit;
                }
            }, TrickleDown.TrickleDown);

            m_fill.RegisterCallback<GeometryChangedEvent>((evt) => UpdateElementPositions());
            m_fill.RegisterCallback<PointerDownEvent>((evt) =>
            {
                if ((Mode == InteractionMode.ThumbsAndSelection || Mode == InteractionMode.SelectionOnly) &&
                    panel.GetCapturingElement(evt.pointerId) == null)
                    PointerCaptureHelper.CapturePointer(m_fill, evt.pointerId);
            });
            m_fill.RegisterCallback<PointerUpEvent>((evt) =>
            {
                PointerCaptureHelper.ReleasePointer(m_fill, evt.pointerId);
            });
            m_fill.RegisterCallback<PointerMoveEvent>((evt) =>
            {
                if ((Mode == InteractionMode.ThumbsAndSelection || Mode == InteractionMode.SelectionOnly) &&
                    PointerCaptureHelper.HasPointerCapture(m_fill, evt.pointerId))
                {
                    float valueDelta = ((HighLimit - LowLimit) * evt.deltaPosition.x / m_track.resolvedStyle.width);
                    if (MinValue + valueDelta >= LowLimit && MaxValue + valueDelta <= HighLimit)
                    {
                        value = new Vector2(value.x + valueDelta, value.y + valueDelta);
                    }
                }
            });

            UpdateElementPositions();
        }

        VisualElement m_visualInput;
        VisualElement m_fill;
        VisualElement m_track;
        VisualElement m_markerContainer;
        VisualElement m_lowHandle;
        VisualElement m_highHandle;

        InteractionMode m_interactionMode = InteractionMode.ThumbsAndSelection;
        public InteractionMode Mode
        {
            get => m_interactionMode;
            set
            {
                if (value != m_interactionMode)
                {
                    m_interactionMode = value;
                }
            }
        }

        /// <summary>
        /// The lower value of the current selection
        /// </summary>
        public float MinValue
        {
            get => value.x;
            set
            {
                if (value >= rawValue.y)
                    return;

                base.value = ClampValues(new Vector2(value, rawValue.y));
                UpdateElementPositions();
            }
        }

        /// <summary>
        /// The upper value of the current selection
        /// </summary>
        public float MaxValue
        {
            get => rawValue.y;
            set
            {
                if (value <= rawValue.x)
                    return;

                base.value = ClampValues(new Vector2(rawValue.x, value));
                UpdateElementPositions();
            }
        }

        /// <summary>
        /// The current value
        /// </summary>
        public override Vector2 value
        {
            get => base.value;
            set
            {
                base.value = ClampValues(value);
                UpdateElementPositions();
            }
        }

        /// <summary>
        /// The distance between MinValue and MaxValue
        /// </summary>
        public float Range
        {
            get => Mathf.Abs(MaxValue - MinValue);
        }

        /// <summary>
        /// The distance between LowLimit and HighLimit
        /// </summary>
        public float TotalRange
        {
            get => Mathf.Abs(HighLimit - LowLimit);
        }

        float m_lowLimit;
        /// <summary>
        /// The lowest possible value that can be selected
        /// </summary>
        public float LowLimit
        {
            get => m_lowLimit;
            set
            {
                if (!Mathf.Approximately(m_lowLimit, value))
                {
                    if (value > m_highLimit)
                    {
                        throw new System.ArgumentException("LowLimit cannot be greater than HighLimit!");
                    }

                    m_lowLimit = value;
                    this.value = rawValue;
                    UpdateElementPositions();
                }
            }
        }

        float m_highLimit;
        /// <summary>
        /// The highest possible value that can be selected
        /// </summary>
        public float HighLimit
        {
            get => m_highLimit;
            set
            {
                if (!Mathf.Approximately(m_highLimit, value))
                {
                    if (value < m_lowLimit)
                    {
                        throw new System.ArgumentException("HighLimit cannot be less than LowLimit!");
                    }

                    m_highLimit = value;
                    this.value = rawValue;
                    UpdateElementPositions();
                }
            }
        }

        /// <summary>
        /// VE for containing markers inside the slider
        /// </summary>
        public VisualElement MarkerContainer
        {
            get => m_markerContainer;
        }

        public override void SetValueWithoutNotify(Vector2 newValue)
        {
            base.SetValueWithoutNotify(newValue);
        }

        /// <summary>
        /// Adds a simple line marker to the slider at the given value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="color"></param>
        /// <returns>The created marker element</returns>
        public VisualElement AddMarker(float value, Color color)
        {
            var m = new VisualElement();
            m.AddToClassList(singleMarkerUssClassName);
            m.style.backgroundColor = color;
            m.style.left = new StyleLength(new Length(100f * (value - LowLimit) / (HighLimit - LowLimit), LengthUnit.Percent));
            m.pickingMode = PickingMode.Ignore;
            MarkerContainer.Add(m);
            return m;
        }

        /// <summary>
        /// Adds a marker which covers a region of values.
        /// </summary>
        /// <param name="v0">The starting value to attach the marker to</param>
        /// <param name="v1">The ending value to attach the marker to</param>
        /// <param name="color"></param>
        /// <returns>The created marker element</returns>
        public VisualElement AddMarker(float v0, float v1, Color color)
        {
            var m = new VisualElement();
            m.AddToClassList(rangeMarkerUssClassName);
            m.style.backgroundColor = color;
            m.style.left = new StyleLength(new Length(100f * (v0 - LowLimit) / (HighLimit - LowLimit), LengthUnit.Percent));
            m.style.width = new StyleLength(new Length(100f * (v1 - v0) / (HighLimit - LowLimit), LengthUnit.Percent));
            m.pickingMode = PickingMode.Ignore;
            MarkerContainer.Add(m);
            return m;
        }

        /// <summary>
        /// Removes all markers which have been added
        /// </summary>
        public void ClearMarkers()
        {
            MarkerContainer.Clear();
        }

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt == null)
            {
                return;
            }

            if (evt.eventTypeId == GeometryChangedEvent.TypeId())
            {
                UpdateElementPositions();
            }
        }

        protected override void UpdateMixedValueContent()
        {
            base.UpdateMixedValueContent();
        }

        Vector2 ClampValues(Vector2 valueToClamp)
        {
            // Make sure the limits are ok...
            if (LowLimit > HighLimit)
            {
                LowLimit = HighLimit;
            }

            Vector2 clampedValue = new Vector2();

            // Make sure the value max is not bigger than the max limit...
            if (valueToClamp.y > HighLimit)
            {
                valueToClamp.y = HighLimit;
            }

            // Clamp both values
            clampedValue.x = Mathf.Clamp(valueToClamp.x, LowLimit, valueToClamp.y);
            clampedValue.y = Mathf.Clamp(valueToClamp.y, valueToClamp.x, HighLimit);
            return clampedValue;
        }

        void UpdateElementPositions()
        {
            if (panel == null)
            {
                return;
            }

            float widthPct = Range / (HighLimit - LowLimit);
            float handleWidth = (m_lowHandle.resolvedStyle.width / m_track.resolvedStyle.width) + (m_highHandle.resolvedStyle.width / m_track.resolvedStyle.width);
            widthPct = Mathf.Clamp(widthPct, 0f, 1f);
            float width = widthPct;

            float leftOffset = (MinValue - LowLimit) / (HighLimit - LowLimit);

            m_fill.style.width = new StyleLength(new Length(100f * width, LengthUnit.Percent));
            m_fill.style.left = new StyleLength(new Length(100f * leftOffset, LengthUnit.Percent));

            m_lowHandle.style.left = 0;
            m_highHandle.style.right = 0;
        }

        /// <summary>
        /// Instantiates a RangeSlider using data read from a UXML file
        /// </summary>
        public new class UxmlFactory : UxmlFactory<RangeSlider, UxmlTraits>
        {
        }

        public new class UxmlTraits : BaseField<Vector2>.UxmlTraits
        {
            UxmlFloatAttributeDescription m_MinValue  = new UxmlFloatAttributeDescription { name = "min-value",  defaultValue = 0 };
            UxmlFloatAttributeDescription m_MaxValue  = new UxmlFloatAttributeDescription { name = "max-value",  defaultValue = 1 };
            UxmlFloatAttributeDescription m_LowLimit  = new UxmlFloatAttributeDescription { name = "low-limit",  defaultValue = 0 };
            UxmlFloatAttributeDescription m_HighLimit = new UxmlFloatAttributeDescription { name = "high-limit", defaultValue = 1 };
            UxmlEnumAttributeDescription<InteractionMode> m_InteractionMode = new UxmlEnumAttributeDescription<InteractionMode> { name = "interaction-mode", defaultValue = InteractionMode.ThumbsAndSelection };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var slider = ve as RangeSlider;
                slider.LowLimit = m_LowLimit.GetValueFromBag(bag, cc);
                slider.HighLimit = m_HighLimit.GetValueFromBag(bag, cc);
                slider.value = new Vector2(m_MinValue.GetValueFromBag(bag, cc), m_MaxValue.GetValueFromBag(bag, cc));
                slider.Mode = m_InteractionMode.GetValueFromBag(bag, cc);
            }
        }
    }
}
