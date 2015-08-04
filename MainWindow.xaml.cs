using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfApplication1 {

    public class Planet {
        private double _mass;
        private Vector _position;
        private Ellipse _image;
        private List<Ellipse> _trail = new List<Ellipse>( );
        private double _size;

        public const double GravityConstant = 0.5;
        private const double pointSize = 6.0;
        private const int maxPoints = 300;
        private const int SizeMultiplier = 2;

        public double Mass { get { return _mass; } }

        public Vector Velocity { get; set; }

        int moveIndex = 0;

        /// <summary>
        /// Center position
        /// </summary>
        public Vector Position {
            get { return _position; }
            set {
                _position = value;
                _image.SetValue( Canvas.LeftProperty, value.X - _size * 0.5 );
                _image.SetValue( Canvas.TopProperty, value.Y - _size * 0.5 );
            }
        }

        public Planet( double mass, Vector position, Vector velocity, Color color, Canvas canvas ) {
            _mass = mass;
            Velocity = velocity;
            _size = Math.Pow( mass, 1.0 / 3 ) * SizeMultiplier; // Cube-root to get size from mass
            _image = new Ellipse { Width = _size, Height = _size, Fill = new SolidColorBrush( color ) };
            canvas.Children.Add( _image );
            this.Position = position;
        }

        public void AccelerateTowards( Planet otherPlanet ) {
            Velocity += AccelerationTowards( otherPlanet );
        }

        public void Move( ) {
            Position += Velocity;
        }

        public void AddTrailPoint( Canvas canvas ) {
            moveIndex++;
            if ( moveIndex % 5 != 0 ) return;
            var trailItem = new Ellipse { Width = pointSize, Height = pointSize, Fill = new SolidColorBrush( Color.FromRgb( 64, 64, 64 ) ) };
            trailItem.SetValue( Canvas.LeftProperty, Position.X - pointSize * 0.5 );
            trailItem.SetValue( Canvas.TopProperty, Position.Y - pointSize * 0.5 );
            _trail.Add( trailItem );
            canvas.Children.Add( trailItem );
            if ( _trail.Count > maxPoints ) {
                canvas.Children.Remove( _trail[0] );
                _trail.RemoveAt( 0 );
            }
        }

        private Vector AccelerationTowards( Planet otherPlanet ) {
            var toOtherVector = otherPlanet.Position - this.Position;
            var distance = toOtherVector.Length;
            var accelerationSize = GravityConstant * otherPlanet.Mass / ( distance * distance );
            return toOtherVector / distance * accelerationSize;
        }

    }

    public partial class MainWindow : Window {

        private IEnumerable<Planet> _planets;

        public MainWindow( ) {
            InitializeComponent( );
            var timer = new DispatcherTimer( new TimeSpan( 0, 0, 0, 0, 20 ), DispatcherPriority.Background, SimulateStep, this.Dispatcher );
            _planets = new Planet[] {
                new Planet( mass: 700.0, position: new Vector(0, 0), velocity: new Vector(0,0), color: Colors.Red, canvas: Space ),
                new Planet( mass: 300.0, position: new Vector(0, -200), velocity: new Vector(1.5, 0), color: Colors.Green, canvas: Space ),
                new Planet( mass: 700.0, position: new Vector(0, 1600), velocity: new Vector(0.8,0), color: Colors.Blue, canvas: Space ),
                new Planet( mass: 3e00.0, position: new Vector(0, 1400), velocity: new Vector(2.3, 0), color: Colors.Yellow, canvas: Space ),
            };
            NormalizePlanetMovement( );
            NormalizePlanetPosition( );
        }

        private void NormalizePlanetPosition( ) {
            var shift = new Vector( SystemParameters.PrimaryScreenWidth * 0.5, SystemParameters.PrimaryScreenHeight * 0.5 ) - CenterOfMass;
            foreach ( var planet in _planets ) {
                planet.Position += shift;
            }
        }

        private Vector CenterOfMass {
            get {
                return _planets.Select( planet => planet.Mass * planet.Position ).
                    Aggregate( new Vector( 0, 0 ), ( v1, v2 ) => v1 + v2 ) / TotalMass;
            }
        }

        /// <summary>
        /// TODO: Make more functional, creating new planet-objects
        /// </summary>
        private void NormalizePlanetMovement( ) {
            var totalVelocity = TotalVelocity;
            foreach ( var planet in _planets ) {
                planet.Velocity -= totalVelocity;
            }
        }

        private Vector TotalVelocity {
            get {
                var totalMomentum = _planets.
                    Select( planet => planet.Mass * planet.Velocity ).
                    Aggregate( new Vector( 0, 0 ), ( v1, v2 ) => v1 + v2 );
                return totalMomentum / TotalMass;
            }
        }

        private double TotalMass {
            get {
                return _planets.Select( planet => planet.Mass ).Sum( );
            }
        }

        /// <summary>
        /// TODO: Make more functional, calculating new state from old state instead
        /// moving planets one-by-one
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void SimulateStep( object sender, EventArgs args ) {
            // First change all velocities based on initial positions...
            foreach ( var planetToMove in _planets ) {
                foreach ( var otherPlanet in _planets ) {
                    if ( !object.Equals( planetToMove, otherPlanet ) ) {
                        planetToMove.AccelerateTowards( otherPlanet );
                    }
                }
            }
            // ...then move all positions in synchronous step
            foreach ( var planetToMove in _planets ) {
                planetToMove.AddTrailPoint( this.Space );
                planetToMove.Move( );
            }
        }

    } // class

} // namespace
