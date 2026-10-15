using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Testing;
using RA_MuniAnnouncements.Api.ErrorHandling;
using RA_MuniAnnouncements.Api.Exceptions;

namespace RA_MuniAnnouncements.Tests;

public class FormsUnavailableExceptionHandlerTests
{
    private readonly FormsUnavailableExceptionHandler _sut = new(new FakeLogger<FormsUnavailableExceptionHandler>());

    [Fact]
    public async Task FormsServiceUnavailable_Returns503()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var handled = await _sut.TryHandleAsync(context, new FormsServiceUnavailableException("down"), default);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    [Fact]
    public async Task OtherException_IsNotHandled()
    {
        var context = new DefaultHttpContext();

        var handled = await _sut.TryHandleAsync(context, new InvalidOperationException(), default);

        Assert.False(handled); // נופל ל-500 הגנרי
    }
}
