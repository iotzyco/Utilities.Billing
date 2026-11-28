using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Utilities.Billing.Api.Messaging;
using Utilities.Billing.Api.Services;
using Utilities.Billing.Contracts;
using Utilities.Common.Messages;

namespace Utilities.Billing.Tests.WarningCleanup;

/// <summary>
/// Pins <see cref="DeviceMessageConsumer.Consume"/>: the nullability cleanup only added <c>!</c> to the device serial
/// passed to <see cref="IAccountsService.MakePaymentAsync"/> (no IL change). A null serial is still forwarded as null.
/// </summary>
public class DeviceMessageConsumerTests
{
    private static ConsumeContext<Batch<DeviceEvent>> CreateContext(params DeviceEvent[] events)
    {
        var items = events.Select(e =>
        {
            var item = new Mock<ConsumeContext<DeviceEvent>>();
            item.Setup(x => x.Message).Returns(e);
            return item.Object;
        }).ToList();

        var batch = new Mock<Batch<DeviceEvent>>();
        batch.Setup(x => x.GetEnumerator()).Returns(() => items.GetEnumerator());

        var context = new Mock<ConsumeContext<Batch<DeviceEvent>>>();
        context.Setup(x => x.Message).Returns(batch.Object);
        return context.Object;
    }

    private static DeviceEvent Event(string? serial, string name, DateTime time, params (string Code, double Value)[] values) => new()
    {
        DeviceId = Guid.NewGuid(),
        DeviceSerial = serial,
        EventName = name,
        Time = time,
        Values = values.ToDictionary(v => v.Code, v => new DeviceEvent.SensorValue { Value = v.Value }),
    };

    [Test]
    public async Task Consume_ForwardsLatestValuePerDeviceAndInput_AndIgnoresOtherEvents()
    {
        var calls = new List<MakePaymentCommand>();
        var accounts = new Mock<IAccountsService>();
        accounts.Setup(x => x.MakePaymentAsync(It.IsAny<MakePaymentCommand>()))
            .Callback<MakePaymentCommand>(calls.Add)
            .ReturnsAsync(new MakePaymentReply());

        var consumer = new DeviceMessageConsumer(NullLogger<DeviceMessageConsumer>.Instance, Mock.Of<IGrainFactory>(), accounts.Object);
        var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await consumer.Consume(CreateContext(
            Event("dev1", "sensor_data", t0.AddMinutes(1), ("in1", 5)),
            Event("dev1", "sensor_data", t0, ("in1", 3), ("in2", 7)),
            Event("dev1", "online", t0.AddMinutes(2), ("in1", 99)),
            Event(null, "sensor_data", t0, ("in3", 1))));

        Assert.That(calls.Select(c => (c.DeviceSerial, c.InputCode, c.IncomingValue)), Is.EquivalentTo(new (string?, string, double)[]
        {
            ("dev1", "in1", 5),
            ("dev1", "in2", 7),
            (null, "in3", 1),
        }));
    }

    [Test]
    public async Task Consume_NoSensorData_DoesNothing()
    {
        var accounts = new Mock<IAccountsService>(MockBehavior.Strict);
        var consumer = new DeviceMessageConsumer(NullLogger<DeviceMessageConsumer>.Instance, Mock.Of<IGrainFactory>(), accounts.Object);

        await consumer.Consume(CreateContext(Event("dev1", "online", DateTime.UtcNow, ("in1", 1))));

        accounts.VerifyNoOtherCalls();
    }

    [Test]
    public void Consume_PaymentFails_Rethrows()
    {
        var accounts = new Mock<IAccountsService>();
        accounts.Setup(x => x.MakePaymentAsync(It.IsAny<MakePaymentCommand>())).ThrowsAsync(new InvalidOperationException("boom"));
        var consumer = new DeviceMessageConsumer(NullLogger<DeviceMessageConsumer>.Instance, Mock.Of<IGrainFactory>(), accounts.Object);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            consumer.Consume(CreateContext(Event("dev1", "sensor_data", DateTime.UtcNow, ("in1", 1)))));
    }
}
