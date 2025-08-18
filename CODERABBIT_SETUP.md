# CodeRabbit Integration Setup

This repository is configured to use CodeRabbit for automated code reviews on pull requests.

## 📚 What is CodeRabbit?

CodeRabbit is an AI-powered code review tool that provides:
- Automated line-by-line code analysis
- Security vulnerability detection
- Performance optimization suggestions
- Best practice recommendations
- Contextual code insights

## 🔧 Configuration Files

### `.coderabbit.yaml`
Main configuration file that defines:
- Language and framework settings (C#/.NET Core)
- Path-specific review instructions
- Focus areas (security, performance, maintainability)
- Custom rules for invoice processing system
- Auto-approval conditions
- Integration settings

### `.github/pull_request_template.md`
Standardized PR template that helps both CodeRabbit and human reviewers by providing:
- Clear change description format
- Comprehensive testing checklist
- Security and performance considerations
- Database change tracking
- Deployment verification steps

### `DEVELOPMENT_GUIDELINES.md`
Comprehensive coding standards and best practices including:
- C# coding conventions
- Database guidelines
- Security requirements
- Performance optimization
- Testing standards
- Domain-specific rules for invoice processing

## 🚀 How It Works

1. **Create Pull Request**: When you open a PR, CodeRabbit automatically starts reviewing
2. **Automated Analysis**: CodeRabbit analyzes changes against configured rules
3. **Review Comments**: Receives detailed feedback on code quality, security, and best practices
4. **Summary Report**: Get high-level summary of changes and potential issues
5. **Continuous Learning**: CodeRabbit learns from your codebase and improves over time

## 🎯 Review Focus Areas

CodeRabbit is configured to pay special attention to:

### Security
- Input validation for file uploads
- SQL injection prevention
- Authentication/authorization
- Sensitive data exposure
- XSS prevention in views

### Performance  
- Database query optimization
- Async/await patterns
- Memory usage
- Large file handling
- Caching strategies

### Invoice Processing Specific
- OCR accuracy considerations
- File upload security
- Audit trail maintenance
- External service resilience
- Business logic correctness

## 📋 Usage Tips

### For Developers
1. **Fill out PR template completely** - helps CodeRabbit provide better context
2. **Add descriptive commit messages** - improves analysis quality
3. **Review CodeRabbit suggestions carefully** - they're tailored to this project
4. **Address security and performance issues first** - these are high priority
5. **Use the checklist** - ensures comprehensive testing

### For Reviewers
1. **Read CodeRabbit summary first** - get overview of changes
2. **Focus on business logic** - CodeRabbit handles syntax and conventions
3. **Verify security recommendations** - especially for file handling
4. **Check domain-specific rules** - invoice processing workflows
5. **Validate test coverage** - ensure adequate testing

## ⚙️ Customization

The configuration can be adjusted by modifying `.coderabbit.yaml`:

### Adding New Rules
```yaml
custom_rules:
  - name: "new_rule_name"
    pattern: "YourPattern|AnotherPattern"
    message: "Your custom review guidance"
```

### Path-Specific Instructions
```yaml
path_instructions:
  - path: "src/YourNewFolder/**"
    instructions: |
      Your specific review requirements
```

### Auto-Approval Settings
```yaml
auto_approve:
  - path: "docs/**"
    conditions:
      - no_code_changes: true
```

## 🔄 Integration Status

- ✅ CodeRabbit authorized for repository
- ✅ Configuration files created
- ✅ Custom rules for invoice processing system
- ✅ Security and performance focus areas defined
- ✅ Path-specific instructions configured
- ✅ PR template with comprehensive checklists
- ✅ Development guidelines documented

## 📞 Support

If you encounter issues with CodeRabbit:

1. **Configuration Issues**: Check `.coderabbit.yaml` syntax
2. **Missing Reviews**: Ensure PR has code changes (not just markdown)
3. **Unexpected Behavior**: Review path patterns and custom rules
4. **Integration Problems**: Check GitHub repository permissions

For more information, visit the [CodeRabbit Documentation](https://docs.coderabbit.ai).

---

**Next Steps**: Create a pull request to test the CodeRabbit integration and verify all configurations are working correctly.
