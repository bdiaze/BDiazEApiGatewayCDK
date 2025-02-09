using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.IAM;
using Constructs;
using CfnAccount = Amazon.CDK.AWS.APIGateway.CfnAccount;
using CfnAccountProps = Amazon.CDK.AWS.APIGateway.CfnAccountProps;
using Amazon.CDK.AWS.Route53;
using Amazon.CDK.AWS.Route53.Targets;
using DomainNameProps = Amazon.CDK.AWS.Apigatewayv2.DomainNameProps;

namespace BDiazEApiGateway
{
    public class BDiazEApiGatewayStack : Stack
    {
        internal BDiazEApiGatewayStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            string appName = System.Environment.GetEnvironmentVariable("APP_NAME")!;
            string domainName = System.Environment.GetEnvironmentVariable("DOMAIN_NAME");
            string subdomainName = System.Environment.GetEnvironmentVariable("SUBDOMAIN_NAME")!;
            string certificateArn = System.Environment.GetEnvironmentVariable("CERTIFICATE_ARN")!;

            ICertificate certificate = Certificate.FromCertificateArn(this, $"{appName}Certificate", certificateArn);
            IHostedZone hostedZone = HostedZone.FromLookup(this, $"{appName}HostedZone", new HostedZoneProviderProps {
                DomainName = domainName
            });

            DomainName domain = new DomainName(this, $"{appName}DomainName", new DomainNameProps {
                DomainName = subdomainName,
                Certificate = certificate
            });

            Role role = new Role(this, $"{appName}Role", new RoleProps {
                AssumedBy = new ServicePrincipal("apigateway.amazonaws.com"),
                ManagedPolicies = new[] {
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AmazonAPIGatewayPushToCloudWatchLogs")
                }
            });

            CfnAccount account = new CfnAccount(this, $"{appName}AccountApiGateway", new CfnAccountProps {
                CloudWatchRoleArn = role.RoleArn
            });

            ARecord record = new ARecord(this, $"{appName}ARecord", new ARecordProps {
                Zone = hostedZone,
                RecordName = subdomainName,
                Target = RecordTarget.FromAlias(new ApiGatewayv2DomainProperties(domain.RegionalDomainName, domain.RegionalHostedZoneId))
            });
        }
    }
}
