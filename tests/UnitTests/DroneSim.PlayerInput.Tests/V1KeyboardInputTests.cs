using DroneSim.Core;
using DroneSim.PlayerInput;
using Moq;
using Silk.NET.Input;
using System.Collections.Generic;
using Xunit;

namespace DroneSim.PlayerInput.Tests;

public class V1KeyboardInputTests
{
    private readonly V1KeyboardInput _input;
    private readonly Mock<IKeyboard> _mockKeyboard;
    private readonly List<Key> _pressedKeys;

    public V1KeyboardInputTests()
    {
        _input = new V1KeyboardInput();
        _mockKeyboard = new Mock<IKeyboard>();
        _pressedKeys = new List<Key>();

        // Setup the mock to use our list of pressed keys
        _mockKeyboard.Setup(k => k.IsKeyPressed(It.IsAny<Key>()))
                     .Returns((Key key) => _pressedKeys.Contains(key));
    }

    [Fact]
    public void Update_WhenKeyIsHeld_ReturnsContinuousValue()
    {
        // Arrange
        _pressedKeys.Add(Key.D); // Hold 'D' for strafe right

        // Act
        ((IPlayerInput)_input).Update(_mockKeyboard.Object);
        var controls = _input.GetFlightControls();

        // Assert
        Assert.Equal(1.0f, controls.StrafeInput);
    }

    [Fact]
    public void Update_WhenKeyIsPressedOnce_FiresSinglePressEvent()
    {
        // Arrange - Frame 1: No keys pressed
        ((IPlayerInput)_input).Update(_mockKeyboard.Object);
        Assert.False(_input.IsDebugTogglePressed());

        // Arrange - Frame 2: F3 key is now pressed
        _pressedKeys.Add(Key.F3);

        // Act
        ((IPlayerInput)_input).Update(_mockKeyboard.Object);

        // Assert
        Assert.True(_input.IsDebugTogglePressed());
    }

    [Fact]
    public void Update_WhenKeyIsHeld_DoesNotFireSinglePressEventTwice()
    {
        // Arrange - Frame 1: F3 key is pressed
        _pressedKeys.Add(Key.F3);
        ((IPlayerInput)_input).Update(_mockKeyboard.Object);
        Assert.True(_input.IsDebugTogglePressed());

        // Arrange - Frame 2: F3 key is still held down
        // The previous keys are now stored in the V1KeyboardInput instance

        // Act
        ((IPlayerInput)_input).Update(_mockKeyboard.Object);

        // Assert
        Assert.False(_input.IsDebugTogglePressed());
    }

    [Fact]
    public void Update_ThrottleStep_IncrementsAndClampsCorrectly()
    {
        // Arrange - Frame 1 (W pressed)
        _pressedKeys.Add(Key.W);
        ((IPlayerInput)_input).Update(_mockKeyboard.Object);
        var controls1 = _input.GetFlightControls();
        Assert.Equal(1, controls1.ThrottleStep);
        _pressedKeys.Clear();

        // Arrange - Frame 2 (Nothing pressed, advance state)
        ((IPlayerInput)_input).Update(_mockKeyboard.Object);

        // Arrange - Frame 3 (S pressed)
        _pressedKeys.Add(Key.S);
        ((IPlayerInput)_input).Update(_mockKeyboard.Object);
        var controls2 = _input.GetFlightControls();
        Assert.Equal(0, controls2.ThrottleStep);
        _pressedKeys.Clear();

        // Arrange - Frame 4 (S pressed again, check clamping)
        ((IPlayerInput)_input).Update(_mockKeyboard.Object); // update previous keys
        _pressedKeys.Add(Key.S);
        ((IPlayerInput)_input).Update(_mockKeyboard.Object);
        var controls3 = _input.GetFlightControls();
        Assert.Equal(0, controls3.ThrottleStep);
    }
} 