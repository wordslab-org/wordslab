using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wordslab.manager.Migrations
{
    /// <inheritdoc />
    public partial class ConfigStore_v083 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HostMachine",
                columns: table => new
                {
                    HostName = table.Column<string>(type: "TEXT", nullable: false),
                    VirtualMachineClusterPath = table.Column<string>(type: "TEXT", nullable: false),
                    VirtualMachineDataPath = table.Column<string>(type: "TEXT", nullable: false),
                    BackupPath = table.Column<string>(type: "TEXT", nullable: false),
                    Processors = table.Column<int>(type: "INTEGER", nullable: false),
                    MemoryGB = table.Column<int>(type: "INTEGER", nullable: false),
                    CanUseGPUs = table.Column<bool>(type: "INTEGER", nullable: false),
                    VirtualMachineClusterSizeGB = table.Column<int>(type: "INTEGER", nullable: false),
                    VirtualMachineDataSizeGB = table.Column<int>(type: "INTEGER", nullable: false),
                    BackupSizeGB = table.Column<int>(type: "INTEGER", nullable: false),
                    SSHPort = table.Column<int>(type: "INTEGER", nullable: false),
                    KubernetesPort = table.Column<int>(type: "INTEGER", nullable: false),
                    HttpPort = table.Column<int>(type: "INTEGER", nullable: false),
                    CanExposeHttpOnLAN = table.Column<bool>(type: "INTEGER", nullable: false),
                    HttpsPort = table.Column<int>(type: "INTEGER", nullable: false),
                    CanExposeHttpsOnLAN = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateTimeCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateTimeUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostMachine", x => x.HostName);
                });

            migrationBuilder.CreateTable(
                name: "VirtualMachine",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Spec_Compute_Processors = table.Column<int>(type: "INTEGER", nullable: false),
                    Spec_Compute_MemoryGB = table.Column<int>(type: "INTEGER", nullable: false),
                    Spec_GPU_ModelName = table.Column<string>(type: "TEXT", nullable: true),
                    Spec_GPU_MemoryGB = table.Column<int>(type: "INTEGER", nullable: false),
                    Spec_GPU_GPUCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Spec_Storage_ClusterDiskSizeGB = table.Column<int>(type: "INTEGER", nullable: false),
                    Spec_Storage_ClusterDiskIsSSD = table.Column<bool>(type: "INTEGER", nullable: false),
                    Spec_Storage_DataDiskSizeGB = table.Column<int>(type: "INTEGER", nullable: false),
                    Spec_Storage_DataDiskIsSSD = table.Column<bool>(type: "INTEGER", nullable: false),
                    Spec_Network_SSHPort = table.Column<int>(type: "INTEGER", nullable: false),
                    Spec_Network_KubernetesPort = table.Column<int>(type: "INTEGER", nullable: false),
                    Spec_Network_HttpIngressPort = table.Column<int>(type: "INTEGER", nullable: false),
                    Spec_Network_HttpsIngressPort = table.Column<int>(type: "INTEGER", nullable: false),
                    VmProvider = table.Column<int>(type: "INTEGER", nullable: false),
                    VmModelName = table.Column<string>(type: "TEXT", nullable: true),
                    IsPreemptible = table.Column<bool>(type: "INTEGER", nullable: false),
                    ForwardSSHPortOnLocalhost = table.Column<bool>(type: "INTEGER", nullable: false),
                    HostSSHPort = table.Column<int>(type: "INTEGER", nullable: false),
                    ForwardKubernetesPortOnLocalhost = table.Column<bool>(type: "INTEGER", nullable: false),
                    HostKubernetesPort = table.Column<int>(type: "INTEGER", nullable: false),
                    ForwardHttpIngressPortOnLocalhost = table.Column<bool>(type: "INTEGER", nullable: false),
                    HostHttpIngressPort = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowHttpAccessFromLAN = table.Column<bool>(type: "INTEGER", nullable: false),
                    ForwardHttpsIngressPortOnLocalhost = table.Column<bool>(type: "INTEGER", nullable: false),
                    HostHttpsIngressPort = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowHttpsAccessFromLAN = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirtualMachine", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "VMInstance",
                columns: table => new
                {
                    DateTimeCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    StartTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ComputeStartArguments_Processors = table.Column<int>(type: "INTEGER", nullable: true),
                    ComputeStartArguments_MemoryGB = table.Column<int>(type: "INTEGER", nullable: true),
                    GPUStartArguments_ModelName = table.Column<string>(type: "TEXT", nullable: true),
                    GPUStartArguments_MemoryGB = table.Column<int>(type: "INTEGER", nullable: true),
                    GPUStartArguments_GPUCount = table.Column<int>(type: "INTEGER", nullable: true),
                    StartArgumentsMessages = table.Column<string>(type: "TEXT", nullable: true),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    VmProcessId = table.Column<int>(type: "INTEGER", nullable: false),
                    VmIPAddress = table.Column<string>(type: "TEXT", nullable: true),
                    Kubeconfig = table.Column<string>(type: "TEXT", nullable: true),
                    StopTimestamp = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExecutionMessages = table.Column<string>(type: "TEXT", nullable: true),
                    DateTimeUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VMInstance", x => new { x.Name, x.DateTimeCreated });
                    table.ForeignKey(
                        name: "FK_VMInstance_VirtualMachine_Name",
                        column: x => x.Name,
                        principalTable: "VirtualMachine",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HostMachine");

            migrationBuilder.DropTable(
                name: "VMInstance");

            migrationBuilder.DropTable(
                name: "VirtualMachine");
        }
    }
}
