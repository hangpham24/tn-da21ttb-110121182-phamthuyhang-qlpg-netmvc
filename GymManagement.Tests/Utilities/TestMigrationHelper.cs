using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GymManagement.Tests.Utilities
{
    /// <summary>
    /// 🔄 TEST MIGRATION HELPER
    /// Utilities to help migrate from Mock-based tests to In-Memory tests
    /// Provides analysis and conversion tools
    /// </summary>
    public static class TestMigrationHelper
    {
        #region Analysis Methods

        /// <summary>
        /// Analyze existing test files to identify Mock usage
        /// </summary>
        public static async Task<MockAnalysisResult> AnalyzeTestFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Test file not found: {filePath}");

            var content = await File.ReadAllTextAsync(filePath);
            var result = new MockAnalysisResult
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath)
            };

            // Analyze Mock usage patterns
            result.MockDeclarations = FindMockDeclarations(content);
            result.MockSetups = FindMockSetups(content);
            result.MockVerifications = FindMockVerifications(content);
            result.TestMethods = FindTestMethods(content);
            
            // Calculate complexity score
            result.ComplexityScore = CalculateComplexityScore(result);
            result.MigrationDifficulty = DetermineMigrationDifficulty(result);
            
            return result;
        }

        /// <summary>
        /// Analyze entire test project for Mock usage
        /// </summary>
        public static async Task<ProjectAnalysisResult> AnalyzeTestProjectAsync(string projectPath)
        {
            var result = new ProjectAnalysisResult
            {
                ProjectPath = projectPath
            };

            var testFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => f.Contains("Test") && !f.Contains("bin") && !f.Contains("obj"))
                .ToList();

            foreach (var testFile in testFiles)
            {
                try
                {
                    var fileAnalysis = await AnalyzeTestFileAsync(testFile);
                    result.FileAnalyses.Add(fileAnalysis);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Error analyzing {testFile}: {ex.Message}");
                }
            }

            // Calculate project-level statistics
            result.TotalTestFiles = result.FileAnalyses.Count;
            result.TotalMockDeclarations = result.FileAnalyses.Sum(f => f.MockDeclarations.Count);
            result.TotalTestMethods = result.FileAnalyses.Sum(f => f.TestMethods.Count);
            result.AverageComplexityScore = result.FileAnalyses.Average(f => f.ComplexityScore);

            return result;
        }

        #endregion

        #region Pattern Recognition

        private static List<MockDeclaration> FindMockDeclarations(string content)
        {
            var declarations = new List<MockDeclaration>();
            
            // Pattern: Mock<IInterface> _mockName;
            var mockPattern = @"Mock<(\w+)>\s+(\w+);";
            var matches = Regex.Matches(content, mockPattern);
            
            foreach (Match match in matches)
            {
                declarations.Add(new MockDeclaration
                {
                    InterfaceName = match.Groups[1].Value,
                    VariableName = match.Groups[2].Value,
                    LineNumber = GetLineNumber(content, match.Index)
                });
            }

            return declarations;
        }

        private static List<MockSetup> FindMockSetups(string content)
        {
            var setups = new List<MockSetup>();
            
            // Pattern: mockName.Setup(x => x.Method()).Returns(value);
            var setupPattern = @"(\w+)\.Setup\(([^)]+)\)\.Returns[^;]*;";
            var matches = Regex.Matches(content, setupPattern);
            
            foreach (Match match in matches)
            {
                setups.Add(new MockSetup
                {
                    MockVariable = match.Groups[1].Value,
                    SetupExpression = match.Groups[2].Value,
                    LineNumber = GetLineNumber(content, match.Index)
                });
            }

            return setups;
        }

        private static List<MockVerification> FindMockVerifications(string content)
        {
            var verifications = new List<MockVerification>();
            
            // Pattern: mockName.Verify(x => x.Method(), Times.Once);
            var verifyPattern = @"(\w+)\.Verify\(([^,]+),\s*([^)]+)\);";
            var matches = Regex.Matches(content, verifyPattern);
            
            foreach (Match match in matches)
            {
                verifications.Add(new MockVerification
                {
                    MockVariable = match.Groups[1].Value,
                    VerifyExpression = match.Groups[2].Value,
                    TimesExpression = match.Groups[3].Value,
                    LineNumber = GetLineNumber(content, match.Index)
                });
            }

            return verifications;
        }

        private static List<TestMethod> FindTestMethods(string content)
        {
            var methods = new List<TestMethod>();
            
            // Pattern: [Fact] or [Theory] followed by method
            var testPattern = @"\[(Fact|Theory)\]\s*public\s+async\s+Task\s+(\w+)\(";
            var matches = Regex.Matches(content, testPattern);
            
            foreach (Match match in matches)
            {
                methods.Add(new TestMethod
                {
                    Name = match.Groups[2].Value,
                    Type = match.Groups[1].Value,
                    LineNumber = GetLineNumber(content, match.Index)
                });
            }

            return methods;
        }

        private static int GetLineNumber(string content, int index)
        {
            return content.Substring(0, index).Count(c => c == '\n') + 1;
        }

        #endregion

        #region Analysis Calculations

        private static int CalculateComplexityScore(MockAnalysisResult result)
        {
            var score = 0;
            
            // Base score from mock declarations
            score += result.MockDeclarations.Count * 2;
            
            // Additional score from setups (more complex)
            score += result.MockSetups.Count * 3;
            
            // Additional score from verifications
            score += result.MockVerifications.Count * 2;
            
            // Bonus for complex interfaces
            var complexInterfaces = new[] { "IUnitOfWork", "IRepository", "IEmailService" };
            score += result.MockDeclarations.Count(d => 
                complexInterfaces.Any(ci => d.InterfaceName.Contains(ci))) * 5;
            
            return score;
        }

        private static MigrationDifficulty DetermineMigrationDifficulty(MockAnalysisResult result)
        {
            if (result.ComplexityScore <= 10)
                return MigrationDifficulty.Easy;
            else if (result.ComplexityScore <= 25)
                return MigrationDifficulty.Medium;
            else if (result.ComplexityScore <= 50)
                return MigrationDifficulty.Hard;
            else
                return MigrationDifficulty.VeryHard;
        }

        #endregion

        #region Migration Suggestions

        /// <summary>
        /// Generate migration suggestions for a test file
        /// </summary>
        public static MigrationSuggestions GenerateMigrationSuggestions(MockAnalysisResult analysis)
        {
            var suggestions = new MigrationSuggestions
            {
                FileName = analysis.FileName,
                Difficulty = analysis.MigrationDifficulty
            };

            // Suggest base class change
            suggestions.Steps.Add(new MigrationStep
            {
                Type = MigrationStepType.ChangeBaseClass,
                Description = "Change base class from TestBase to InMemoryTestBase",
                Priority = MigrationPriority.High,
                EstimatedEffort = "5 minutes"
            });

            // Suggest removing mock declarations
            foreach (var mock in analysis.MockDeclarations)
            {
                suggestions.Steps.Add(new MigrationStep
                {
                    Type = MigrationStepType.RemoveMockDeclaration,
                    Description = $"Remove Mock<{mock.InterfaceName}> {mock.VariableName} declaration",
                    Priority = MigrationPriority.High,
                    EstimatedEffort = "2 minutes",
                    LineNumber = mock.LineNumber
                });
            }

            // Suggest replacing mock setups with real data
            foreach (var setup in analysis.MockSetups)
            {
                suggestions.Steps.Add(new MigrationStep
                {
                    Type = MigrationStepType.ReplaceSetupWithData,
                    Description = $"Replace {setup.MockVariable}.Setup() with real test data creation",
                    Priority = MigrationPriority.Medium,
                    EstimatedEffort = "10 minutes",
                    LineNumber = setup.LineNumber
                });
            }

            // Suggest replacing verifications with assertions
            foreach (var verify in analysis.MockVerifications)
            {
                suggestions.Steps.Add(new MigrationStep
                {
                    Type = MigrationStepType.ReplaceVerificationWithAssertion,
                    Description = $"Replace {verify.MockVariable}.Verify() with database state assertions",
                    Priority = MigrationPriority.Medium,
                    EstimatedEffort = "8 minutes",
                    LineNumber = verify.LineNumber
                });
            }

            // Calculate total effort
            var totalMinutes = suggestions.Steps.Sum(s => ParseEffortMinutes(s.EstimatedEffort));
            suggestions.TotalEstimatedEffort = $"{totalMinutes} minutes ({totalMinutes / 60.0:F1} hours)";

            return suggestions;
        }

        private static int ParseEffortMinutes(string effort)
        {
            var match = Regex.Match(effort, @"(\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : 10;
        }

        #endregion

        #region Report Generation

        /// <summary>
        /// Generate migration report
        /// </summary>
        public static async Task<string> GenerateMigrationReportAsync(ProjectAnalysisResult projectAnalysis)
        {
            var report = new StringBuilder();
            
            report.AppendLine("# 🔄 MOCK TO IN-MEMORY MIGRATION REPORT");
            report.AppendLine();
            report.AppendLine($"**Generated**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"**Project**: {Path.GetFileName(projectAnalysis.ProjectPath)}");
            report.AppendLine();
            
            // Executive Summary
            report.AppendLine("## 📊 EXECUTIVE SUMMARY");
            report.AppendLine();
            report.AppendLine($"- **Total Test Files**: {projectAnalysis.TotalTestFiles}");
            report.AppendLine($"- **Total Mock Declarations**: {projectAnalysis.TotalMockDeclarations}");
            report.AppendLine($"- **Total Test Methods**: {projectAnalysis.TotalTestMethods}");
            report.AppendLine($"- **Average Complexity Score**: {projectAnalysis.AverageComplexityScore:F1}");
            report.AppendLine();
            
            // Difficulty Distribution
            var difficultyGroups = projectAnalysis.FileAnalyses
                .GroupBy(f => f.MigrationDifficulty)
                .ToDictionary(g => g.Key, g => g.Count());
            
            report.AppendLine("## 🎯 MIGRATION DIFFICULTY DISTRIBUTION");
            report.AppendLine();
            foreach (var difficulty in Enum.GetValues<MigrationDifficulty>())
            {
                var count = difficultyGroups.GetValueOrDefault(difficulty, 0);
                var percentage = projectAnalysis.TotalTestFiles > 0 ? (count * 100.0 / projectAnalysis.TotalTestFiles) : 0;
                report.AppendLine($"- **{difficulty}**: {count} files ({percentage:F1}%)");
            }
            report.AppendLine();
            
            // File-by-File Analysis
            report.AppendLine("## 📁 FILE-BY-FILE ANALYSIS");
            report.AppendLine();
            
            foreach (var fileAnalysis in projectAnalysis.FileAnalyses.OrderByDescending(f => f.ComplexityScore))
            {
                report.AppendLine($"### {fileAnalysis.FileName}");
                report.AppendLine();
                report.AppendLine($"- **Complexity Score**: {fileAnalysis.ComplexityScore}");
                report.AppendLine($"- **Migration Difficulty**: {fileAnalysis.MigrationDifficulty}");
                report.AppendLine($"- **Mock Declarations**: {fileAnalysis.MockDeclarations.Count}");
                report.AppendLine($"- **Mock Setups**: {fileAnalysis.MockSetups.Count}");
                report.AppendLine($"- **Mock Verifications**: {fileAnalysis.MockVerifications.Count}");
                report.AppendLine($"- **Test Methods**: {fileAnalysis.TestMethods.Count}");
                report.AppendLine();
                
                if (fileAnalysis.MockDeclarations.Any())
                {
                    report.AppendLine("**Mocked Interfaces**:");
                    foreach (var mock in fileAnalysis.MockDeclarations)
                    {
                        report.AppendLine($"  - {mock.InterfaceName} ({mock.VariableName})");
                    }
                    report.AppendLine();
                }
            }
            
            // Migration Recommendations
            report.AppendLine("## 🚀 MIGRATION RECOMMENDATIONS");
            report.AppendLine();
            
            var easyFiles = projectAnalysis.FileAnalyses.Where(f => f.MigrationDifficulty == MigrationDifficulty.Easy).ToList();
            var mediumFiles = projectAnalysis.FileAnalyses.Where(f => f.MigrationDifficulty == MigrationDifficulty.Medium).ToList();
            var hardFiles = projectAnalysis.FileAnalyses.Where(f => f.MigrationDifficulty >= MigrationDifficulty.Hard).ToList();
            
            report.AppendLine("### Phase 1: Easy Migrations (Start Here)");
            foreach (var file in easyFiles.Take(5))
            {
                report.AppendLine($"- {file.FileName} (Score: {file.ComplexityScore})");
            }
            report.AppendLine();
            
            report.AppendLine("### Phase 2: Medium Complexity");
            foreach (var file in mediumFiles.Take(5))
            {
                report.AppendLine($"- {file.FileName} (Score: {file.ComplexityScore})");
            }
            report.AppendLine();
            
            report.AppendLine("### Phase 3: Complex Migrations (Require Planning)");
            foreach (var file in hardFiles.Take(5))
            {
                report.AppendLine($"- {file.FileName} (Score: {file.ComplexityScore})");
            }
            report.AppendLine();
            
            // Errors
            if (projectAnalysis.Errors.Any())
            {
                report.AppendLine("## ⚠️ ANALYSIS ERRORS");
                report.AppendLine();
                foreach (var error in projectAnalysis.Errors)
                {
                    report.AppendLine($"- {error}");
                }
                report.AppendLine();
            }
            
            return report.ToString();
        }

        #endregion
    }

    #region Supporting Classes

    public class MockAnalysisResult
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public List<MockDeclaration> MockDeclarations { get; set; } = new();
        public List<MockSetup> MockSetups { get; set; } = new();
        public List<MockVerification> MockVerifications { get; set; } = new();
        public List<TestMethod> TestMethods { get; set; } = new();
        public int ComplexityScore { get; set; }
        public MigrationDifficulty MigrationDifficulty { get; set; }
    }

    public class ProjectAnalysisResult
    {
        public string ProjectPath { get; set; } = string.Empty;
        public List<MockAnalysisResult> FileAnalyses { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public int TotalTestFiles { get; set; }
        public int TotalMockDeclarations { get; set; }
        public int TotalTestMethods { get; set; }
        public double AverageComplexityScore { get; set; }
    }

    public class MockDeclaration
    {
        public string InterfaceName { get; set; } = string.Empty;
        public string VariableName { get; set; } = string.Empty;
        public int LineNumber { get; set; }
    }

    public class MockSetup
    {
        public string MockVariable { get; set; } = string.Empty;
        public string SetupExpression { get; set; } = string.Empty;
        public int LineNumber { get; set; }
    }

    public class MockVerification
    {
        public string MockVariable { get; set; } = string.Empty;
        public string VerifyExpression { get; set; } = string.Empty;
        public string TimesExpression { get; set; } = string.Empty;
        public int LineNumber { get; set; }
    }

    public class TestMethod
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int LineNumber { get; set; }
    }

    public class MigrationSuggestions
    {
        public string FileName { get; set; } = string.Empty;
        public MigrationDifficulty Difficulty { get; set; }
        public List<MigrationStep> Steps { get; set; } = new();
        public string TotalEstimatedEffort { get; set; } = string.Empty;
    }

    public class MigrationStep
    {
        public MigrationStepType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public MigrationPriority Priority { get; set; }
        public string EstimatedEffort { get; set; } = string.Empty;
        public int LineNumber { get; set; }
    }

    public enum MigrationDifficulty
    {
        Easy,
        Medium,
        Hard,
        VeryHard
    }

    public enum MigrationStepType
    {
        ChangeBaseClass,
        RemoveMockDeclaration,
        ReplaceSetupWithData,
        ReplaceVerificationWithAssertion,
        AddTestDataCreation,
        UpdateAssertions
    }

    public enum MigrationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    #endregion
}
