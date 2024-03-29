﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows;
using System.Windows.Media.Animation;

namespace HappyFriendMemoryGame.ControlContent3D
{
            /// <summary>
        /// Contains the available destinations for a rotation of ContentControl3D.
        /// A value of this enumeration can be passed as a parameter to the RotateCommand.
        /// </summary>
        public enum RotationDestination
        {
            /// <summary>
            /// The rotation will bring the back side of the 3D surface into view.
            /// If the back side is already in view, the rotation will not occur.
            /// </summary>
            BackSide,

            /// <summary>
            /// The rotation will bring the front side of the 3D surface into view.
            /// If the front side is already in view, the rotation will not occur.
            /// </summary>
            FrontSide
        }

        /// <summary>
	    /// A ContentControl that provides the ability to rotate in 3D to show its BackContent.
	    /// </summary>
        [TemplatePart(Name = "PART_Viewport", Type = typeof(Viewport3D))]
        [TemplatePart(Name = "PART_FrontContentPresenter", Type = typeof(ContentPresenter))]
        [TemplatePart(Name = "PART_BackContentPresenter", Type = typeof(ContentPresenter))]
        public class ContentControl3D : ContentControl
        {

            #region Fields

            AxisAngleRotation3D _backRotation;
            PerspectiveCamera _defaultCameraPrototype;
            AxisAngleRotation3D _frontRotation;
            bool _isRotationPending;
            Viewport3D _viewport;

            #endregion // Fields

            #region Constructors

            static ContentControl3D()
            {
                DefaultStyleKeyProperty.OverrideMetadata(
                    typeof(ContentControl3D),
                    new FrameworkPropertyMetadata(typeof(ContentControl3D)));

                AnimationLengthProperty =
                    DependencyProperty.Register(
                    "AnimationLength",
                    typeof(int),
                    typeof(ContentControl3D),
                    new UIPropertyMetadata(600, OnAnimationLengthChanged));

                BackContentProperty = DependencyProperty.Register(
                    "BackContent",
                    typeof(object),
                    typeof(ContentControl3D));

                BackContentTemplateProperty = DependencyProperty.Register(
                    "BackContentTemplate",
                    typeof(DataTemplate),
                    typeof(ContentControl3D));

                CameraPrototypeProperty = DependencyProperty.Register(
                    "CameraPrototype",
                    typeof(PerspectiveCamera),
                    typeof(ContentControl3D),
                    new UIPropertyMetadata(null, OnCameraPrototypeChanged));

                CameraZoomDestinationProperty = DependencyProperty.Register(
                    "CameraZoomDestination",
                    typeof(Point3D),
                    typeof(ContentControl3D),
                    new UIPropertyMetadata(new Point3D(0, 0, 2.5)));

                CanRotateProperty = DependencyProperty.Register(
                    "CanRotate",
                    typeof(bool),
                    typeof(ContentControl3D),
                    new UIPropertyMetadata(true, OnCanRotateChanged));

                IsFrontInViewPropertyKey = DependencyProperty.RegisterReadOnly(
                    "IsFrontInView",
                    typeof(bool),
                    typeof(ContentControl3D),
                    new UIPropertyMetadata(true));
                IsFrontInViewProperty = IsFrontInViewPropertyKey.DependencyProperty;

                IsOnFrontSidePropertyKey = DependencyProperty.RegisterAttachedReadOnly(
                    "IsOnFrontSide",
                    typeof(Nullable<bool>),
                    typeof(ContentControl3D),
                    new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
                IsOnFrontSideProperty = IsOnFrontSidePropertyKey.DependencyProperty;

                IsRotatingPropertyKey = DependencyProperty.RegisterReadOnly(
                    "IsRotating",
                    typeof(bool),
                    typeof(ContentControl3D),
                    new UIPropertyMetadata(false));
                IsRotatingProperty = IsRotatingPropertyKey.DependencyProperty;
            }

            public ContentControl3D()
            {
                base.RequestBringIntoView += this.OnRequestBringIntoView;
                base.CommandBindings.Add(new CommandBinding(RotateCommand, OnRotateCommandExecuted, OnCanExecuteRotateCommand));
                this.IsFrontInView = true;
            }

            #endregion // Constructors

            #region Methods

            #region BringBackSideIntoView

            /// <summary>
            /// Ensures that back side of the surface is facing the user.
            /// </summary>
            public void BringBackSideIntoView()
            {
                // Note: IsFrontInView is set to true/false once an animation completes,
                // but this code might be invoked during an animation.
                if ((!this.IsFrontInView && !this.IsRotating) || (this.IsFrontInView && this.IsRotating))
                    return;

                if (!this.IsFrontInView && this.IsRotating)
                {
                    // If we call the Rotate method while the surface is rotating, it will be ignored.
                    _isRotationPending = true;
                    return;
                }

                this.Rotate();
            }

            #endregion // BringBackSideIntoView

            #region BringFrontSideIntoView

            /// <summary>
            /// Ensures that front side of the surface is facing the user.
            /// </summary>
            public void BringFrontSideIntoView()
            {
                // Note: IsFrontInView is set to true/false once an animation completes,
                // but this code might be invoked during an animation.
                if ((this.IsFrontInView && !this.IsRotating) || (!this.IsFrontInView && this.IsRotating))
                    return;

                if (this.IsFrontInView && this.IsRotating)
                {
                    // If we call the Rotate method while the surface is rotating, it will be ignored.
                    _isRotationPending = true;
                    return;
                }

                this.Rotate();
            }

            #endregion // BringFrontSideIntoView

            #region Rotate

            /// <summary>
            /// Rotates the control in 3D space over the amount of time specified by the AnimationLength property.
            /// </summary>
            public void Rotate()
            {
                if (!this.CanRotate)
                    throw new InvalidOperationException("You cannot call the Rotate method of ContentControl3D when CanRotate is set to false.");

                if (_viewport == null)
                    throw new InvalidOperationException("The ContentControl3D's control template does not contain a Viewport3D whose name is PART_Viewport.");

                if (_frontRotation == null)
                    throw new InvalidOperationException("The ContentControl3D's control template does not contain a Visual2DViewport3D with an AxisAngleRotation3D for the front side.");

                if (_backRotation == null)
                    throw new InvalidOperationException("The ContentControl3D's control template does not contain a Visual2DViewport3D with an AxisAngleRotation3D for the back side.");

                if (this.IsRotating)
                    return;

                // Avoid trying to animate a null or frozen instance.
                if (_viewport.Camera == null || _viewport.Camera.IsFrozen)
                    _viewport.Camera = this.CreateCamera();

                PerspectiveCamera camera = _viewport.Camera as PerspectiveCamera;

                // Create the animations.
                DoubleAnimation frontAnimation, backAnimation;
                this.PrepareForRotation(out frontAnimation, out backAnimation);
                Point3DAnimation cameraZoomAnim = this.CreateCameraAnimation();

                // Start the animations.
                _frontRotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, frontAnimation);
                _backRotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, backAnimation);
                camera.BeginAnimation(PerspectiveCamera.PositionProperty, cameraZoomAnim);

                this.IsRotating = true;
            }

            #endregion // Rotate

            #endregion // Methods

            #region Commands

            #region Rotate Command

            /// <summary>
            /// Executing this routed command causes the 3D surface to rotate.
            /// You can pass an optional RotationDestination value as the command
            /// parameter, to ensure that a certain side of the surface is in view.
            /// </summary>
            public static readonly RoutedCommand RotateCommand = new RoutedCommand();

            void OnCanExecuteRotateCommand(object sender, CanExecuteRoutedEventArgs e)
            {
                e.CanExecute = this.CanRotate;
            }

            void OnRotateCommandExecuted(object sender, ExecutedRoutedEventArgs e)
            {
                if (!this.CanRotate)
                    return;

                if (e.Parameter is RotationDestination)
                {
                    RotationDestination destination = (RotationDestination)e.Parameter;
                    if (destination == RotationDestination.FrontSide)
                        this.BringFrontSideIntoView();
                    else
                        this.BringBackSideIntoView();
                }
                else
                {
                    this.Rotate();
                }
            }

            #endregion // Rotate Command

            #endregion // Commands

            #region Dependency Properties

            #region AnimationLength

            /// <summary>
            /// Gets/sets the number of milliseconds it should take to flip the 3D surface over.
            /// This property cannot be set to a value less than 10.
            /// This is a dependency property.
            /// </summary>
            public int AnimationLength
            {
                get { return (int)GetValue(AnimationLengthProperty); }
                set { SetValue(AnimationLengthProperty, value); }
            }

            public static readonly DependencyProperty AnimationLengthProperty;

            static void OnAnimationLengthChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
            {
                int value = (int)e.NewValue;
                if (value < 10)
                    throw new ArgumentOutOfRangeException("AnimationLength", "AnimationLength cannot be less than 10 milliseconds.");
            }

            #endregion // AnimationLength

            #region BackContent

            /// <summary>
            /// Gets/sets the object to display on the back side of the 3D surface.
            /// This is a dependency property.
            /// </summary>
            public object BackContent
            {
                get { return (object)GetValue(BackContentProperty); }
                set { SetValue(BackContentProperty, value); }
            }

            public static readonly DependencyProperty BackContentProperty;

            #endregion // BackContent

            #region BackContentTemplate

            /// <summary>
            /// Gets/sets the optional DataTemplate used to render the BackContent.
            /// This is a dependency property.
            /// </summary>
            public DataTemplate BackContentTemplate
            {
                get { return (DataTemplate)GetValue(BackContentTemplateProperty); }
                set { SetValue(BackContentTemplateProperty, value); }
            }

            public static readonly DependencyProperty BackContentTemplateProperty;

            #endregion // BackContentTemplate

            #region CameraPrototype

            /// <summary>
            /// Gets/sets the PerspectiveCamera that is cloned to provide the 3D scene with a camera.
            /// The default camera is configured as:
            /// &lt;PerspectiveCamera Position="0, 0, 1.2" LookDirection="0, 0, -1" FieldOfView="100" /&gt;
            /// This is a dependency property.
            /// </summary>
            public PerspectiveCamera CameraPrototype
            {
                get { return (PerspectiveCamera)GetValue(CameraPrototypeProperty); }
                set { SetValue(CameraPrototypeProperty, value); }
            }

            public static readonly DependencyProperty CameraPrototypeProperty;

            static void OnCameraPrototypeChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
            {
                ContentControl3D cc3D = depObj as ContentControl3D;
                if (cc3D != null && cc3D._viewport != null)
                    cc3D._viewport.Camera = cc3D.CreateCamera();
            }

            #endregion // CameraPrototype

            #region CameraZoomDestination

            /// <summary>
            /// Gets/sets the peak point, in 3D space, to which the scene's camera
            /// will travel during the animated rotation.  The default value is (0, 0, 2.5).
            /// This is a dependency property.
            /// </summary>
            public Point3D CameraZoomDestination
            {
                get { return (Point3D)GetValue(CameraZoomDestinationProperty); }
                set { SetValue(CameraZoomDestinationProperty, value); }
            }

            public static readonly DependencyProperty CameraZoomDestinationProperty;

            #endregion // CameraZoomDestination

            #region CanRotate

            /// <summary>
            /// Gets/sets whether the surface should be allowed to rotate.
            /// This is a dependency property.
            /// </summary>
            public bool CanRotate
            {
                get { return (bool)GetValue(CanRotateProperty); }
                set { SetValue(CanRotateProperty, value); }
            }

            public static readonly DependencyProperty CanRotateProperty;

            static void OnCanRotateChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
            {
                CommandManager.InvalidateRequerySuggested();
            }

            #endregion // CanRotate

            #region IsFrontInView

            /// <summary>
            /// Returns true if the front side of the 3D surface is currently in view.  This is a read-only dependency property.
            /// </summary>
            public bool IsFrontInView
            {
                get { return (bool)GetValue(IsFrontInViewProperty); }
                private set { SetValue(IsFrontInViewPropertyKey, value); }
            }

            public static readonly DependencyProperty IsFrontInViewProperty;
            private static readonly DependencyPropertyKey IsFrontInViewPropertyKey;

            #endregion // IsFrontInView

            #region IsRotating

            /// <summary>
            /// Returns true if the 3D surface is currently rotating.
            /// This is a read-only dependency property.
            /// </summary>
            public bool IsRotating
            {
                get { return (bool)GetValue(IsRotatingProperty); }
                private set { SetValue(IsRotatingPropertyKey, value); }
            }

            private static readonly DependencyPropertyKey IsRotatingPropertyKey;
            public static readonly DependencyProperty IsRotatingProperty;

            #endregion // IsRotating

            #endregion // Dependency Properties

            #region Attached Properties

            #region IsOnFrontSide

            public static bool? GetIsOnFrontSide(DependencyObject obj)
            {
                return (bool?)obj.GetValue(IsOnFrontSideProperty);
            }

            internal static void SetIsOnFrontSide(DependencyObject obj, bool? value)
            {
                obj.SetValue(IsOnFrontSidePropertyKey, value);
            }

            /// <summary>
            /// Identifies the IsOnFrontSide read-only attached property.
            /// </summary>
            public static readonly DependencyProperty IsOnFrontSideProperty;

            static readonly DependencyPropertyKey IsOnFrontSidePropertyKey;

            #endregion // IsOnFrontSide

            #endregion // Attached Properties

            #region Base Class Overrides

            #region OnApplyTemplate

            public override void OnApplyTemplate()
            {
                base.OnApplyTemplate();

                _viewport = base.Template.FindName("PART_Viewport", this) as Viewport3D;
                ContentPresenter backContentPresenter = base.Template.FindName("PART_BackContentPresenter", this) as ContentPresenter;
                ContentPresenter frontContentPresenter = base.Template.FindName("PART_FrontContentPresenter", this) as ContentPresenter;

                if (_viewport != null)
                    _viewport.Camera = this.CreateCamera();
                else
                    return;

                // Set the IsOnFrontSide attached property on these two ContentPresenters
                // so that Triggers in DataTemplates can rely on that value.  Refer to the
                // ContentValueConverter class for details about how extra support is needed
                // for discovering which side an element is on, from code.

                if (backContentPresenter != null)
                {
                    SetIsOnFrontSide(backContentPresenter, false);
                    _backRotation = this.FindAxisAngleRotation3D(1);
                }

                if (frontContentPresenter != null)
                {
                    SetIsOnFrontSide(frontContentPresenter, true);
                    _frontRotation = this.FindAxisAngleRotation3D(2);
                }
            }

            #endregion // OnApplyTemplate

            #endregion // Base Class Overrides

            #region Private Helpers

            #region CreateCamera

            PerspectiveCamera CreateCamera()
            {
                if (this.CameraPrototype != null)
                    return this.CameraPrototype.Clone();
                else
                    return this.DefaultCameraPrototype.Clone();
            }

            #endregion // CreateCamera

            #region CreateCameraAnimation

            Point3DAnimation CreateCameraAnimation()
            {
                Point3DAnimation cameraZoomAnim = new Point3DAnimation
                {
                    To = this.CameraZoomDestination,
                    Duration = new Duration(TimeSpan.FromMilliseconds(this.AnimationLength / 2)),
                    AutoReverse = true,
                    FillBehavior = FillBehavior.Stop
                };
                cameraZoomAnim.Completed += this.OnRotationCompleted;
                return cameraZoomAnim;
            }

            #endregion // CreateCameraAnimation

            #region DefaultCameraPrototype

            PerspectiveCamera DefaultCameraPrototype
            {
                get
                {
                    if (_defaultCameraPrototype == null)
                    {
                        _defaultCameraPrototype = new PerspectiveCamera
                        {
                            Position = new Point3D(0, 0, 1.2),
                            LookDirection = new Vector3D(0, 0, -1),
                            FieldOfView = 100
                        };
                    }
                    return _defaultCameraPrototype;
                }
            }

            #endregion // DefaultCameraPrototype

            #region FindAxisAngleRotation3D

            AxisAngleRotation3D FindAxisAngleRotation3D(int index)
            {
                if (_viewport.Children.Count <= index)
                    return null;

                Viewport2DVisual3D contentSurface = _viewport.Children[index] as Viewport2DVisual3D;
                if (contentSurface == null)
                    return null;

                RotateTransform3D transform = contentSurface.Transform as RotateTransform3D;
                if (transform == null)
                    return null;

                return transform.Rotation as AxisAngleRotation3D;
            }

            #endregion // FindAxisAngleRotation3D

            #region OnRequestBringIntoView

            void OnRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
            {
                bool? targetIsOnFrontSide = GetIsOnFrontSide(e.TargetObject);
                if (!targetIsOnFrontSide.HasValue)
                    return;

                if (targetIsOnFrontSide.Value)
                    this.BringFrontSideIntoView();
                else
                    this.BringBackSideIntoView();
            }

            #endregion // OnRequestBringIntoView

            #region OnRotationCompleted

            void OnRotationCompleted(object sender, EventArgs e)
            {
                AnimationClock clock = sender as AnimationClock;
                clock.Completed -= this.OnRotationCompleted;

                this.IsRotating = false;
                this.IsFrontInView = !this.IsFrontInView;

                if (_isRotationPending)
                {
                    // The BringFrontSideIntoView/BringBackSideIntoView method was called
                    // during a rotation, and the appropriate side is not in view, so rotate again.
                    _isRotationPending = false;
                    this.Rotate();
                }
                else
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }

            #endregion // OnRotationCompleted

            #region PrepareForRotation

            void PrepareForRotation(out DoubleAnimation frontAnimation, out DoubleAnimation backAnimation)
            {
                Vector3D axis;
                double delta;

                axis = new Vector3D(0, +1, 0);
                delta = +180;

                _frontRotation.Axis = _backRotation.Axis = axis;

                frontAnimation = new DoubleAnimation
                {
                    Duration = new Duration(TimeSpan.FromMilliseconds(this.AnimationLength)),
                    From = _frontRotation.Angle,
                    To = _frontRotation.Angle + delta
                };

                backAnimation = new DoubleAnimation
                {
                    Duration = new Duration(TimeSpan.FromMilliseconds(this.AnimationLength)),
                    From = _backRotation.Angle,
                    To = _backRotation.Angle + delta
                };
            }

            #endregion // PrepareForRotation

            #endregion // Private Helpers
        }
}
