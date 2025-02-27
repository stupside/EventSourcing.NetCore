using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Core.Testing;
using FluentAssertions;
using Meetings.IntegrationTests.MeetingsManagement;
using MeetingsManagement.Api;
using MeetingsManagement.Meetings.Commands;
using MeetingsManagement.Meetings.ValueObjects;
using MeetingsManagement.Meetings.Views;
using Xunit;

namespace MeetingsManagement.IntegrationTests.Meetings
{
    public class ScheduleMeetingFixture: ApiFixture<Startup>
    {
        protected override string ApiUrl => MeetingsManagementApi.MeetingsUrl;

        public readonly Guid MeetingId = Guid.NewGuid();
        public readonly string MeetingName = "Event Sourcing Workshop";
        public readonly DateTime Start = DateTime.UtcNow;
        public readonly DateTime End = DateTime.UtcNow;

        public HttpResponseMessage CreateMeetingCommandResponse = default!;
        public HttpResponseMessage ScheduleMeetingCommandResponse = default!;

        public override async Task InitializeAsync()
        {
            // prepare command
            var createCommand = new CreateMeeting(
                MeetingId,
                MeetingName
            );

            // send create command
            CreateMeetingCommandResponse = await Post( createCommand);

            var occurs = DateRange.Create(Start, End);

            // send schedule meeting request
            ScheduleMeetingCommandResponse = await Post($"{MeetingId}/schedule", occurs);
        }
    }

    public class ScheduleMeetingTests: IClassFixture<ScheduleMeetingFixture>
    {
        private readonly ScheduleMeetingFixture fixture;

        public ScheduleMeetingTests(ScheduleMeetingFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        [Trait("Category", "Exercise")]
        public async Task CreateMeeting_ShouldReturn_CreatedStatus_With_MeetingId()
        {
            var commandResponse = fixture.CreateMeetingCommandResponse.EnsureSuccessStatusCode();
            commandResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdId = await commandResponse.GetResultFromJson<Guid>();
            createdId.Should().NotBeEmpty();
        }

        [Fact]
        [Trait("Category", "Exercise")]
        public async Task ScheduleMeeting_ShouldSucceed()
        {
            var commandResponse = fixture.ScheduleMeetingCommandResponse.EnsureSuccessStatusCode();
            commandResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var createdId = await commandResponse.GetResultFromJson<string>();
            createdId.Should().BeNull();
        }

        [Fact]
        [Trait("Category", "Exercise")]
        public async Task ScheduleMeeting_ShouldUpdateReadModel()
        {
            //send query
            var queryResponse = await fixture.Get($"{fixture.MeetingId}");
            queryResponse.EnsureSuccessStatusCode();

            var meeting = await queryResponse.GetResultFromJson<MeetingView>();
            meeting.Id.Should().Be(fixture.MeetingId);
            meeting.Name.Should().Be(fixture.MeetingName);
            meeting.Start.Should().Be(fixture.Start);
            meeting.End.Should().Be(fixture.End);
        }
    }
}
