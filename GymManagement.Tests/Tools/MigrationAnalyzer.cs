using GymManagement.Tests.Utilities;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GymManagement.Tests.Tools
{
    /// <summary>
    /// 🔧 MIGRATION ANALYZER CONSOLE TOOL
    /// Command-line tool to analyze and plan Mock to In-Memory migration
    /// Usage: dotnet run --project MigrationAnalyzer [project-path]
    /// </summary>
    public class MigrationAnalyzer
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🔄 MOCK TO IN-MEMORY MIGRATION ANALYZER");
            Console.WriteLine("=====================================");
            Console.WriteLine();

            try
            {
                var projectPath = GetProjectPath(args);
                
                if (!Directory.Exists(projectPath))
                {
                    Console.WriteLine($"❌ Project path not found: {projectPath}");
                    return;
                }

                Console.WriteLine($"📁 Analyzing project: {projectPath}");
                Console.WriteLine("⏳ This may take a few moments...");
                Console.WriteLine();

                // Perform analysis
                var analysis = await TestMigrationHelper.AnalyzeTestProjectAsync(projectPath);
                
                // Display results
                await DisplayAnalysisResults(analysis);
                
                // Generate report
                await GenerateReports(analysis);
                
                Console.WriteLine();
                Console.WriteLine("✅ Analysis completed successfully!");
                Console.WriteLine("📄 Check the generated reports for detailed migration plan.");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during analysis: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static string GetProjectPath(string[] args)
        {
            if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            {
                return Path.GetFullPath(args[0]);
            }

            // Default to current directory
            return Directory.GetCurrentDirectory();
        }

        private static async Task DisplayAnalysisResults(ProjectAnalysisResult analysis)
        {
            Console.WriteLine("📊 ANALYSIS RESULTS");
            Console.WriteLine("==================");
            Console.WriteLine();

            // Summary statistics
            Console.WriteLine($"📁 Total Test Files: {analysis.TotalTestFiles}");
            Console.WriteLine($"🎭 Total Mock Declarations: {analysis.TotalMockDeclarations}");
            Console.WriteLine($"🧪 Total Test Methods: {analysis.TotalTestMethods}");
            Console.WriteLine($"📈 Average Complexity Score: {analysis.AverageComplexityScore:F1}");
            Console.WriteLine();

            // Difficulty distribution
            Console.WriteLine("🎯 MIGRATION DIFFICULTY DISTRIBUTION");
            Console.WriteLine("-----------------------------------");
            
            var difficultyGroups = analysis.FileAnalyses
                .GroupBy(f => f.MigrationDifficulty)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var difficulty in Enum.GetValues<MigrationDifficulty>())
            {
                var count = difficultyGroups.GetValueOrDefault(difficulty, 0);
                var percentage = analysis.TotalTestFiles > 0 ? (count * 100.0 / analysis.TotalTestFiles) : 0;
                var emoji = GetDifficultyEmoji(difficulty);
                
                Console.WriteLine($"{emoji} {difficulty}: {count} files ({percentage:F1}%)");
            }
            Console.WriteLine();

            // Top complex files
            Console.WriteLine("🔥 TOP 5 MOST COMPLEX FILES");
            Console.WriteLine("---------------------------");
            
            var topComplexFiles = analysis.FileAnalyses
                .OrderByDescending(f => f.ComplexityScore)
                .Take(5)
                .ToList();

            foreach (var file in topComplexFiles)
            {
                var difficultyEmoji = GetDifficultyEmoji(file.MigrationDifficulty);
                Console.WriteLine($"{difficultyEmoji} {file.FileName} (Score: {file.ComplexityScore}, {file.MigrationDifficulty})");
                Console.WriteLine($"   📦 Mocks: {file.MockDeclarations.Count}, ⚙️ Setups: {file.MockSetups.Count}, ✅ Verifications: {file.MockVerifications.Count}");
            }
            Console.WriteLine();

            // Errors
            if (analysis.Errors.Any())
            {
                Console.WriteLine("⚠️ ANALYSIS ERRORS");
                Console.WriteLine("------------------");
                foreach (var error in analysis.Errors)
                {
                    Console.WriteLine($"❌ {error}");
                }
                Console.WriteLine();
            }

            // Migration recommendations
            Console.WriteLine("🚀 MIGRATION RECOMMENDATIONS");
            Console.WriteLine("============================");
            Console.WriteLine();

            var easyFiles = analysis.FileAnalyses.Where(f => f.MigrationDifficulty == MigrationDifficulty.Easy).Count();
            var mediumFiles = analysis.FileAnalyses.Where(f => f.MigrationDifficulty == MigrationDifficulty.Medium).Count();
            var hardFiles = analysis.FileAnalyses.Where(f => f.MigrationDifficulty >= MigrationDifficulty.Hard).Count();

            Console.WriteLine("📅 SUGGESTED MIGRATION PHASES:");
            Console.WriteLine();
            Console.WriteLine($"🟢 Phase 1 (Week 1): Start with {easyFiles} easy files");
            Console.WriteLine($"🟡 Phase 2 (Week 2): Migrate {mediumFiles} medium complexity files");
            Console.WriteLine($"🔴 Phase 3 (Week 3): Handle {hardFiles} complex files (requires planning)");
            Console.WriteLine();

            // Effort estimation
            var totalEstimatedHours = CalculateTotalEffort(analysis);
            Console.WriteLine($"⏱️ ESTIMATED TOTAL EFFORT: {totalEstimatedHours:F1} hours");
            Console.WriteLine($"👥 RECOMMENDED TEAM SIZE: {GetRecommendedTeamSize(totalEstimatedHours)} developers");
            Console.WriteLine($"📆 ESTIMATED TIMELINE: {GetEstimatedTimeline(totalEstimatedHours)} weeks");
        }

        private static async Task GenerateReports(ProjectAnalysisResult analysis)
        {
            Console.WriteLine();
            Console.WriteLine("📄 GENERATING REPORTS");
            Console.WriteLine("=====================");

            try
            {
                // Generate main migration report
                var reportContent = await TestMigrationHelper.GenerateMigrationReportAsync(analysis);
                var reportPath = Path.Combine(analysis.ProjectPath, "MIGRATION_REPORT.md");
                await File.WriteAllTextAsync(reportPath, reportContent);
                Console.WriteLine($"✅ Migration report saved: {reportPath}");

                // Generate file-specific suggestions
                var suggestionsDir = Path.Combine(analysis.ProjectPath, "MigrationSuggestions");
                Directory.CreateDirectory(suggestionsDir);

                foreach (var fileAnalysis in analysis.FileAnalyses.Where(f => f.MockDeclarations.Any()))
                {
                    var suggestions = TestMigrationHelper.GenerateMigrationSuggestions(fileAnalysis);
                    var suggestionContent = GenerateSuggestionReport(suggestions);
                    
                    var suggestionPath = Path.Combine(suggestionsDir, $"{Path.GetFileNameWithoutExtension(fileAnalysis.FileName)}_Migration.md");
                    await File.WriteAllTextAsync(suggestionPath, suggestionContent);
                }

                Console.WriteLine($"✅ Individual migration suggestions saved in: {suggestionsDir}");

                // Generate CSV summary for tracking
                var csvPath = Path.Combine(analysis.ProjectPath, "MIGRATION_TRACKING.csv");
                await GenerateTrackingCsv(analysis, csvPath);
                Console.WriteLine($"✅ Migration tracking CSV saved: {csvPath}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating reports: {ex.Message}");
            }
        }

        private static string GenerateSuggestionReport(MigrationSuggestions suggestions)
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine($"# 🔄 Migration Plan: {suggestions.FileName}");
            report.AppendLine();
            report.AppendLine($"**Difficulty**: {suggestions.Difficulty}");
            report.AppendLine($"**Estimated Effort**: {suggestions.TotalEstimatedEffort}");
            report.AppendLine();
            
            report.AppendLine("## 📋 Migration Steps");
            report.AppendLine();
            
            var stepNumber = 1;
            foreach (var step in suggestions.Steps.OrderBy(s => s.Priority))
            {
                var priorityEmoji = step.Priority switch
                {
                    MigrationPriority.Critical => "🔴",
                    MigrationPriority.High => "🟡",
                    MigrationPriority.Medium => "🟢",
                    _ => "⚪"
                };
                
                report.AppendLine($"### {stepNumber}. {priorityEmoji} {step.Description}");
                report.AppendLine();
                report.AppendLine($"- **Priority**: {step.Priority}");
                report.AppendLine($"- **Estimated Effort**: {step.EstimatedEffort}");
                if (step.LineNumber > 0)
                {
                    report.AppendLine($"- **Line Number**: {step.LineNumber}");
                }
                report.AppendLine();
                
                stepNumber++;
            }
            
            return report.ToString();
        }

        private static async Task GenerateTrackingCsv(ProjectAnalysisResult analysis, string csvPath)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("FileName,Difficulty,ComplexityScore,MockCount,SetupCount,VerificationCount,TestMethodCount,EstimatedHours,Status");
            
            foreach (var file in analysis.FileAnalyses)
            {
                var estimatedHours = CalculateFileEffort(file);
                csv.AppendLine($"{file.FileName},{file.MigrationDifficulty},{file.ComplexityScore},{file.MockDeclarations.Count},{file.MockSetups.Count},{file.MockVerifications.Count},{file.TestMethods.Count},{estimatedHours:F1},Pending");
            }
            
            await File.WriteAllTextAsync(csvPath, csv.ToString());
        }

        private static string GetDifficultyEmoji(MigrationDifficulty difficulty)
        {
            return difficulty switch
            {
                MigrationDifficulty.Easy => "🟢",
                MigrationDifficulty.Medium => "🟡",
                MigrationDifficulty.Hard => "🔴",
                MigrationDifficulty.VeryHard => "⚫",
                _ => "⚪"
            };
        }

        private static double CalculateTotalEffort(ProjectAnalysisResult analysis)
        {
            return analysis.FileAnalyses.Sum(f => CalculateFileEffort(f));
        }

        private static double CalculateFileEffort(MockAnalysisResult file)
        {
            // Base effort calculation
            var baseHours = file.MigrationDifficulty switch
            {
                MigrationDifficulty.Easy => 0.5,
                MigrationDifficulty.Medium => 2.0,
                MigrationDifficulty.Hard => 4.0,
                MigrationDifficulty.VeryHard => 8.0,
                _ => 1.0
            };

            // Additional effort based on complexity
            var complexityMultiplier = 1.0 + (file.ComplexityScore / 100.0);
            
            return baseHours * complexityMultiplier;
        }

        private static int GetRecommendedTeamSize(double totalHours)
        {
            if (totalHours <= 40) return 1;
            if (totalHours <= 80) return 2;
            if (totalHours <= 160) return 3;
            return 4;
        }

        private static int GetEstimatedTimeline(double totalHours)
        {
            var weeksPerDeveloper = totalHours / 20; // Assuming 20 hours per week per developer
            var teamSize = GetRecommendedTeamSize(totalHours);
            return Math.Max(1, (int)Math.Ceiling(weeksPerDeveloper / teamSize));
        }
    }
}
