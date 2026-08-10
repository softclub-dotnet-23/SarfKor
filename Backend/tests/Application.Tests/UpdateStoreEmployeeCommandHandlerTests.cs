using Application.Abstractions;
using Application.Stores.Commands.UpdateStoreEmployee;
using Domain.Stores;
using Domain.ValueObjects;
using Moq;

namespace Application.Tests;

public class UpdateStoreEmployeeCommandHandlerTests
{
    private const string OwnerId = "owner-1";
    private const int StoreId = 1;
    private const int StoreEmployeeId = 5;

    private readonly Mock<IStoreEmployeeRepository> _storeEmployeeRepository = new();
    private readonly Mock<IStoreAccessAuthorizer> _storeAccessAuthorizer = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UpdateStoreEmployeeCommandHandler CreateHandler() => new(_storeEmployeeRepository.Object, _storeAccessAuthorizer.Object, _unitOfWork.Object);

    private static StoreEmployee CreateEmployee() => new()
    {
        Id = StoreEmployeeId,
        StoreId = StoreId,
        UserId = "cashier-1",
        Role = StoreEmployeeRole.Cashier,
        AddedAt = DateTimeOffset.UtcNow
    };

    private static UpdateStoreEmployeeCommand ValidCommand() =>
        new(StoreEmployeeId, 1500m, "TJS", new TimeOnly(9, 0), new TimeOnly(18, 0), null, null, null, OwnerId);

    [Fact]
    public async Task Handle_EmployeeNotFound_ReturnsNotFound()
    {
        _storeEmployeeRepository.Setup(r => r.GetByIdAsync(StoreEmployeeId, It.IsAny<CancellationToken>())).ReturnsAsync((StoreEmployee?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(UpdateStoreEmployeeOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsForbidden()
    {
        _storeEmployeeRepository.Setup(r => r.GetByIdAsync(StoreEmployeeId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateEmployee());

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand() with { PerformedByUserId = "someone-else" }, CancellationToken.None);

        Assert.Equal(UpdateStoreEmployeeOutcome.Forbidden, result.Outcome);
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsSalaryAndSchedule()
    {
        var employee = CreateEmployee();
        _storeEmployeeRepository.Setup(r => r.GetByIdAsync(StoreEmployeeId, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(UpdateStoreEmployeeOutcome.Updated, result.Outcome);
        Assert.Equal(1500m, employee.MonthlySalary?.Amount);
        Assert.Equal("TJS", employee.MonthlySalary?.Currency);
        Assert.Equal(new TimeOnly(9, 0), employee.ScheduleStart);
        Assert.Equal(new TimeOnly(18, 0), employee.ScheduleEnd);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullsPassed_ClearsExistingSalaryAndSchedule()
    {
        var employee = CreateEmployee();
        employee.MonthlySalary = new Money(1000, "TJS");
        employee.ScheduleStart = new TimeOnly(8, 0);
        employee.ScheduleEnd = new TimeOnly(17, 0);
        _storeEmployeeRepository.Setup(r => r.GetByIdAsync(StoreEmployeeId, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        _storeAccessAuthorizer.Setup(a => a.IsOwnerAsync(StoreId, OwnerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _storeAccessAuthorizer.Setup(a => a.IsOperationalAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = CreateHandler();
        var command = new UpdateStoreEmployeeCommand(StoreEmployeeId, null, null, null, null, null, null, null, OwnerId);
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(UpdateStoreEmployeeOutcome.Updated, result.Outcome);
        Assert.Null(employee.MonthlySalary);
        Assert.Null(employee.ScheduleStart);
        Assert.Null(employee.ScheduleEnd);
    }
}
