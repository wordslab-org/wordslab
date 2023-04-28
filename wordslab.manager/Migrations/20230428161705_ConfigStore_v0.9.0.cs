using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wordslab.manager.Migrations
{
    /// <inheritdoc />
    public partial class ConfigStore_v090 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_VMInstance",
                table: "VMInstance");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VMInstance",
                table: "VMInstance",
                columns: new[] { "Name", "StartTimestamp" });

            migrationBuilder.CreateTable(
                name: "ContainerImage",
                columns: table => new
                {
                    Digest = table.Column<string>(type: "TEXT", nullable: false),
                    Registry = table.Column<string>(type: "TEXT", nullable: false),
                    Repository = table.Column<string>(type: "TEXT", nullable: false),
                    Tag = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContainerImage", x => x.Digest);
                });

            migrationBuilder.CreateTable(
                name: "ImageLayer",
                columns: table => new
                {
                    Digest = table.Column<string>(type: "TEXT", nullable: false),
                    MediaType = table.Column<string>(type: "TEXT", nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageLayer", x => x.Digest);
                });

            migrationBuilder.CreateTable(
                name: "KubernetesApp",
                columns: table => new
                {
                    YamlFileHash = table.Column<string>(type: "TEXT", nullable: false),
                    VirtualMachineName = table.Column<string>(type: "TEXT", nullable: false),
                    YamlFileURL = table.Column<string>(type: "TEXT", nullable: false),
                    InstallDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsFullyDownloadedInContentStore = table.Column<bool>(type: "INTEGER", nullable: false),
                    RemainingDownloadSize = table.Column<long>(type: "INTEGER", nullable: false),
                    UninstallDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateTimeCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateTimeUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NamespaceDefault = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<string>(type: "TEXT", nullable: false),
                    HomePage = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Author = table.Column<string>(type: "TEXT", nullable: false),
                    Licence = table.Column<string>(type: "TEXT", nullable: false),
                    YamlFileContent = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KubernetesApp", x => new { x.VirtualMachineName, x.YamlFileHash });
                });

            migrationBuilder.CreateTable(
                name: "ContainerImageInfoContainerImageLayerInfo",
                columns: table => new
                {
                    LayersDigest = table.Column<string>(type: "TEXT", nullable: false),
                    UsedByContainerImagesDigest = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContainerImageInfoContainerImageLayerInfo", x => new { x.LayersDigest, x.UsedByContainerImagesDigest });
                    table.ForeignKey(
                        name: "FK_ContainerImageInfoContainerImageLayerInfo_ContainerImage_UsedByContainerImagesDigest",
                        column: x => x.UsedByContainerImagesDigest,
                        principalTable: "ContainerImage",
                        principalColumn: "Digest",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContainerImageInfoContainerImageLayerInfo_ImageLayer_LayersDigest",
                        column: x => x.LayersDigest,
                        principalTable: "ImageLayer",
                        principalColumn: "Digest",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppDeployment",
                columns: table => new
                {
                    VirtualMachineName = table.Column<string>(type: "TEXT", nullable: false),
                    Namespace = table.Column<string>(type: "TEXT", nullable: false),
                    AppVirtualMachineName = table.Column<string>(type: "TEXT", nullable: true),
                    AppYamlFileHash = table.Column<string>(type: "TEXT", nullable: true),
                    DeploymentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RemovalDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateTimeCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateTimeUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppDeployment", x => new { x.VirtualMachineName, x.Namespace });
                    table.ForeignKey(
                        name: "FK_AppDeployment_KubernetesApp_AppVirtualMachineName_AppYamlFileHash",
                        columns: x => new { x.AppVirtualMachineName, x.AppYamlFileHash },
                        principalTable: "KubernetesApp",
                        principalColumns: new[] { "VirtualMachineName", "YamlFileHash" });
                });

            migrationBuilder.CreateTable(
                name: "ContainerImageInfoKubernetesAppInstall",
                columns: table => new
                {
                    ContainerImagesDigest = table.Column<string>(type: "TEXT", nullable: false),
                    UsedByKubernetesAppsVirtualMachineName = table.Column<string>(type: "TEXT", nullable: false),
                    UsedByKubernetesAppsYamlFileHash = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContainerImageInfoKubernetesAppInstall", x => new { x.ContainerImagesDigest, x.UsedByKubernetesAppsVirtualMachineName, x.UsedByKubernetesAppsYamlFileHash });
                    table.ForeignKey(
                        name: "FK_ContainerImageInfoKubernetesAppInstall_ContainerImage_ContainerImagesDigest",
                        column: x => x.ContainerImagesDigest,
                        principalTable: "ContainerImage",
                        principalColumn: "Digest",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContainerImageInfoKubernetesAppInstall_KubernetesApp_UsedByKubernetesAppsVirtualMachineName_UsedByKubernetesAppsYamlFileHash",
                        columns: x => new { x.UsedByKubernetesAppsVirtualMachineName, x.UsedByKubernetesAppsYamlFileHash },
                        principalTable: "KubernetesApp",
                        principalColumns: new[] { "VirtualMachineName", "YamlFileHash" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppDeployment_AppVirtualMachineName_AppYamlFileHash",
                table: "AppDeployment",
                columns: new[] { "AppVirtualMachineName", "AppYamlFileHash" });

            migrationBuilder.CreateIndex(
                name: "IX_ContainerImageInfoContainerImageLayerInfo_UsedByContainerImagesDigest",
                table: "ContainerImageInfoContainerImageLayerInfo",
                column: "UsedByContainerImagesDigest");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerImageInfoKubernetesAppInstall_UsedByKubernetesAppsVirtualMachineName_UsedByKubernetesAppsYamlFileHash",
                table: "ContainerImageInfoKubernetesAppInstall",
                columns: new[] { "UsedByKubernetesAppsVirtualMachineName", "UsedByKubernetesAppsYamlFileHash" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppDeployment");

            migrationBuilder.DropTable(
                name: "ContainerImageInfoContainerImageLayerInfo");

            migrationBuilder.DropTable(
                name: "ContainerImageInfoKubernetesAppInstall");

            migrationBuilder.DropTable(
                name: "ImageLayer");

            migrationBuilder.DropTable(
                name: "ContainerImage");

            migrationBuilder.DropTable(
                name: "KubernetesApp");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VMInstance",
                table: "VMInstance");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VMInstance",
                table: "VMInstance",
                columns: new[] { "Name", "DateTimeCreated" });
        }
    }
}
