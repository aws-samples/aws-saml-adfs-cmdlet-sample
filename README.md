# aws-saml-adfs-cmdlet-sample
A reference Windows PowerShell module that obtains and sets temporary AWS security credentials in a Windows PowerShell session using SAML and AD FS. The code can be adapted for use in any C# .NET application. 

## Prerequisites

To use the cmdlet, you must have:

- Active Directory Federation Services (AD FS) correctly integrated with your AWS account for console access using only your organizational credentials. See [Enabling Federation to AWS Using Windows Active Directory, AD FS, and SAML 2.0](http://blogs.aws.amazon.com/security/post/Tx71TWXXJ3UI14/Enabling-Federation-to-AWS-using-Windows-Active-Directory-ADFS-and-SAML-2-0), if you need instructions about this integration. Note that these steps are similar if you're using AD FS 3.0.
- To run, the latest version of the [AWS Tools for Windows PowerShell](https://aws.amazon.com/powershell/) installed on your local workstation.
- To compile, the latest version of the [AWS SDK for .NET](https://aws.amazon.com/sdk-for-net/) installed on your local workstation.

## How to compile the PowerShell module
```
PS > C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe .\AWSSAMLCredentials\AWSSAMLCredentials.sln /p:Configuration=Release
```

## How to use the Set-AWSSAMLCredentials cmdlet

```
PS > Import-Module .\AWSSAMLCredentials\ClassLibrary1\bin\Release\AWSSAML.dll
```

Now, let's look at how we authenticate with the AD FS identity provider to obtain temporary AWS credentials. Using the `AWSSAMLCredentials` cmdlet, we can interactively provide Windows Active Directory credentials and then select an AWS role to which the user has access.

When running the `Set-AWSSAMLCredentials` cmdlet be sure to replace the **example AD FS hostname** with your own actual AD FS hostname.

```
PS > Set-AWSSAMLCredentials -IdentityProviderUrl "https://**adfs.example.com**/adfs/ls/IdpInitiatedSignOn.aspx?loginToRp=urn:amazon:webservices" -UseCurrentCredentials $false
```

```
username: adminaduser
password: ************
domain: example.com

Please choose the role you would like to assume:
  [0]: arn:aws:iam::012345678912:role/ADFS-Production
  [1]: arn:aws:iam::012345678912:role/ADFS-Dev

Selection: 0
```

Rather than typing your Windows credentials every time, the `Set-AWSSAMLCredentials` cmdlet can obtain temporary AWS credentials using your existing Active Directory credentials.

```
PS > Set-AWSSAMLCredentials -IdentityProviderUrl "https://**adfs.example.com**/adfs/ls/IdpInitiatedSignOn.aspx?loginToRp=urn:amazon:webservices" -UseCurrentCredentials $true
```

```
Please choose the role you would like to assume:
  [0]: arn:aws:iam::012345678912:role/ADFS-Production
  [1]: arn:aws:iam::012345678912:role/ADFS-Dev

Selection: 0
```

To obtain temporary AWS credentials non-interactively, the `RoleIndex` parameter can be used to select an AWS role. In the following command, we obtain temporary credentials by using one of the roles you have pre-configured in AD FS (such as the `ADFS-Production` role [item 0 in the list above]).

```
PS > Set-AWSSAMLCredentials -IdentityProviderUrl "https://**adfs.example.com**/adfs/ls/IdpInitiatedSignOn.aspx?loginToRp=urn:amazon:webservices" -RoleIndex 0
```

Now let's use the temporary AWS credentials obtained by using the `Set-AWSSAMLCredentials` cmdlet to interact with AWS service APIs.

**Example 1:** In this example, we will list all the available Amazon S3 buckets in the AWS account of the role we have assumed. This is a common task for administrators managing S3 from the Windows PowerShell command line.
```
 PS > Get-S3Bucket
 
 CreationDate                                                BucketName
 ------------                                                ----------
 7/25/2013 3:16:56 AM                                        mybucket1
 4/15/2015 12:46:50 AM                                       mybucket2
 4/15/2015 6:15:53 AM                                        mybucket3
 1/12/2015 11:20:16 PM                                       mybucket4
```

Notice how we didn't need to provide credentials when we called `Get-S3Bucket` cmdlet. Running the `Set-AWSSAMLCredentials` cmdlet has made temporary credentials available to the AWS Tools for Windows in the current PowerShell session. These credentials will expire after 1 hour. When the credentials expire, the Windows PowerShell module can be rerun to refresh the credentials without any user interaction. Note that I have again selected the role using the `RoleIndex` parameter.

```
PS > Set-AWSSAMLCredentials –IdentityProviderUrl "https://**adfs.example.com**/adfs/ls/IdpInitiatedSignOn.aspx?loginToRp=urn:amazon:webservices" -UseCurrentCredentials $true –RoleIndex 0
```

**Example 2:** Now let's list all Amazon EC2 instances in the Sydney region. You may want to do this to get a list of all the EC2 instances in the region in order to manage your EC2 fleet.

```
PS > (Get-Ec2Instance –Region ap-southeast-2).Instances | Select InstanceType, @{Name="Servername";Expression={$\_.tags | where key -eq "Name" | Select Value -Expand Value}}

 InstanceType                                                Servername
 ------------                                                ----------
 t2.small                                                    DC2
 t1.micro                                                    NAT1
 t1.micro                                                    RDGW1
 t1.micro                                                    RDGW2
 t1.micro                                                    NAT2
 t2.small                                                    DC1
 t2.micro                                                    BUILD
```

## License

This sample application is distributed under the
[Apache License, Version 2.0](http://www.apache.org/licenses/LICENSE-2.0).

