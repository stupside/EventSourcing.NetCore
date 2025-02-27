using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Core.Commands;
using Core.Repositories;
using Marten;
using MediatR;
using Npgsql;
using SmartHome.Temperature.MotionSensors.Commands;

namespace SmartHome.Temperature.MotionSensors
{
    public class MotionSensorCommandHandler :
        ICommandHandler<InstallMotionSensor>,
        ICommandHandler<RebuildMotionSensorsViews>
    {
        private readonly IRepository<MotionSensor> repository;
        private readonly IDocumentSession session;

        public MotionSensorCommandHandler(
            IRepository<MotionSensor> repository,
            IDocumentSession session
        )
        {
            Guard.Against.Null(repository, nameof(repository));

            this.repository = repository;
            this.session = session;
        }

        public async Task<Unit> Handle(InstallMotionSensor command, CancellationToken cancellationToken)
        {
            Guard.Against.Null(command, nameof(command));

            var reservation = MotionSensor.Install(
                command.MotionSensorId
            );

            await repository.Add(reservation, cancellationToken);

            return Unit.Value;
        }

        public async Task<Unit> Handle(RebuildMotionSensorsViews command, CancellationToken cancellationToken)
        {
            var cmd = new NpgsqlCommand("DELETE FROM smart_home_read.mt_doc_motionsensor", session.Connection);
            await cmd.ExecuteNonQueryAsync(cancellationToken);

            Guard.Against.Null(command, nameof(command));

            using (var daemon = session.DocumentStore.BuildProjectionDaemon())
            {
                await daemon.RebuildProjection<MotionSensor>(cancellationToken);
            }
            return Unit.Value;
        }
    }
}
