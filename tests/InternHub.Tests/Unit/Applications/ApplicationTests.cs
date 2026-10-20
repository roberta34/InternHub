using InternHub.Api.Domain.Applications;

namespace InternHub.Tests.Unit.Applications;

public class ApplicationTests
{
    [Fact]
    public void Constructor_ValidData_CreatesSubmittedApplication()
    {
        var id = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var applicationDate = DateTime.UtcNow;

        var application = new Application(id, studentId, offerId, applicationDate);

        Assert.Equal(id, application.Id);
        Assert.Equal(studentId, application.StudentId);
        Assert.Equal(offerId, application.OfferId);
        Assert.Equal(applicationDate, application.ApplicationDate);
        Assert.Equal(ApplicationStatus.Submitted, application.Status);
    }

    [Fact]
    public void Constructor_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Application(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_EmptyStudentId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Application(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_EmptyOfferId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Application(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, DateTime.UtcNow));
    }
}