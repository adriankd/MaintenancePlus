# Pull Request Template for Vehicle Maintenance Invoice System
# This template will be used by CodeRabbit and team members for consistent PR reviews

## 📋 Description
Brief description of changes and their purpose.

## 🔧 Type of Change
- [ ] Bug fix (non-breaking change that fixes an issue)
- [ ] New feature (non-breaking change that adds functionality)  
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update
- [ ] Performance improvement
- [ ] Security enhancement
- [ ] UI/UX improvement
- [ ] Database schema change
- [ ] Configuration change

## 🎯 Related Issues
Fixes #(issue number)
Closes #(issue number)
Addresses #(issue number)

## 🧪 Testing
### Test Coverage
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing completed
- [ ] All existing tests pass

### Test Scenarios
Describe the test scenarios covered:
- [ ] Happy path scenarios
- [ ] Error handling scenarios
- [ ] Edge cases
- [ ] Performance testing (if applicable)
- [ ] Security testing (if applicable)

## 🔍 Code Review Checklist
### General
- [ ] Code follows project coding standards
- [ ] Self-review completed
- [ ] Comments added for complex logic
- [ ] No debugging code left behind
- [ ] Error handling implemented appropriately

### Security (for invoice processing system)
- [ ] Input validation implemented
- [ ] File upload security measures in place
- [ ] No sensitive data exposed in logs
- [ ] Database queries use parameterized statements
- [ ] Authentication/authorization properly implemented

### Performance
- [ ] Database queries optimized
- [ ] Async/await patterns used correctly
- [ ] Memory usage considered
- [ ] Large file handling optimized (if applicable)

### Invoice Processing Specific
- [ ] OCR accuracy considerations addressed
- [ ] Invoice data validation implemented
- [ ] Audit trail maintained for approvals
- [ ] Error handling for external AI services
- [ ] Blob storage operations secure and efficient

## 📊 Database Changes
### Migrations
- [ ] Migration script created and tested
- [ ] Migration is reversible
- [ ] Data integrity maintained
- [ ] Performance impact assessed

### Schema Changes
- [ ] Database indexes updated if needed
- [ ] Foreign key constraints properly defined
- [ ] Data types appropriate for use case

## 🎨 UI/UX Changes
- [ ] Responsive design maintained
- [ ] Accessibility standards followed
- [ ] Browser compatibility tested
- [ ] User experience improved
- [ ] Consistent with design system

## 📚 Documentation
- [ ] README updated (if applicable)
- [ ] API documentation updated (if applicable)
- [ ] Code comments added for complex logic
- [ ] Configuration documentation updated

## 🚀 Deployment
### Pre-deployment Checklist
- [ ] Environment variables configured
- [ ] Database migration plan ready
- [ ] Rollback plan prepared
- [ ] Monitoring/alerting updated

### Post-deployment Verification
- [ ] Health checks pass
- [ ] Key functionality verified
- [ ] Performance monitoring reviewed
- [ ] Error logs reviewed

## 🔄 Dependencies
### New Dependencies
List any new packages or dependencies added:
- Package name: Version (reason for addition)

### Updated Dependencies
List any updated dependencies:
- Package name: Old version → New version (reason for update)

## 📝 Additional Notes
Any additional context, concerns, or areas that need special attention during review.

## 🏷️ Labels
Suggested labels for this PR:
- [ ] enhancement
- [ ] bug
- [ ] security
- [ ] performance
- [ ] documentation
- [ ] breaking-change
- [ ] needs-testing
- [ ] ui-change
- [ ] database-change

---
**Note**: This PR will be automatically reviewed by CodeRabbit. Please ensure all checklist items are completed before requesting human review.
