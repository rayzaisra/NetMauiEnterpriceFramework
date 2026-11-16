using Microsoft.EntityFrameworkCore;

using __PROJECT__.Domain.Entities;
using __PROJECT__.Domain.Enums;

namespace __PROJECT__.Infrastructure.Data;

public static class ParameterSeeder
{
    private static readonly (string TableName, string Description, string Icon)[] Cards =
    {
        ("rf_default_value", "Parameter Default", "SettingsApplications"),
        ("rf_email_provider", "Email Provider", "Email"),
        ("rf_email_template", "Email Template", "Email"),
        ("rf_files_upload_type", "Tipe Jenis File yang diupload", "FileCopy"),
        ("rf_group_access_report_set", "Akses Group untuk Report", "GroupWork"),
        ("rf_message", "Pesan", "Comment"),
        ("tb_parameter_system", "Parameter System", "SystemUpdateAlt"),
        ("tb_parameter_system_detail", "Parameter System Detail", "SettingsSystemDaydream"),
        ("tb_report_set", "Report Set", "Report"),
        ("tb_screen_menu", "Screen Menu", "Settings"),
    };

    public static async Task SeedAsync(__PROJECT__DbContext context, CancellationToken cancellationToken = default)
    {
        await SeedCardsAsync(context, cancellationToken);
        await SeedFieldsAsync(context, cancellationToken);
        await SeedReferenceDataAsync(context, cancellationToken);
        await SeedScreensAsync(context, cancellationToken);
        await SeedReportAsync(context, cancellationToken);
        await SeedAccessAsync(context, cancellationToken);
    }

    private static async Task SeedCardsAsync(__PROJECT__DbContext context, CancellationToken cancellationToken)
    {
        foreach (var card in Cards)
        {
            if (await context.tb_parameter_system.AnyAsync(e => e.table_name == card.TableName, cancellationToken))
            {
                continue;
            }

            await context.tb_parameter_system.AddAsync(new tb_parameter_system
            {
                table_name = card.TableName,
                description = card.Description,
                param_maker_url = card.TableName == "tb_screen_menu"
                    ? "/dashboard/maintenance/screen-menu-editor"
                    : $"/dashboard/maintenance/parameter-system/maker/{card.TableName}",
                param_approval_url = $"/dashboard/maintenance/parameter-system/approval/{card.TableName}",
                is_param_maker = true,
                is_param_approval = true,
                is_param_upload = false,
                icon = card.Icon,
            }, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedFieldsAsync(__PROJECT__DbContext context, CancellationToken cancellationToken)
    {
        await AddFieldsAsync(context, "rf_default_value", cancellationToken, new[]
        {
            Field("def_code", "Code", "text", key: true, mandatory: true, order: 1),
            Field("def_name", "Name", "text", order: 2),
            Field("def_value", "Value", "text", order: 3),
            Field("is_active", "Active", "bit", order: 4),
        });

        await AddFieldsAsync(context, "rf_email_template", cancellationToken, new[]
        {
            Field("template_key", "Template Key", "text", key: true, mandatory: true, order: 1),
            Field("subject", "Subject", "text", mandatory: true, order: 2),
            Field("body_html", "Body HTML", "html", order: 3),
            Field("body_text", "Body Text", "textarea", order: 4),
            Field("is_active", "Active", "bit", order: 5),
        });

        await AddFieldsAsync(context, "rf_email_provider", cancellationToken, new[]
        {
            Field("id", "ID", "number", key: true, order: 1),
            Field("provider_type", "Type", "list", mandatory: true, order: 2,
                reff: "select N'SMTP' as value, N'SMTP' as text union all select N'GMAIL' as value, N'GMAIL' as text"),
            Field("provider_name", "Name", "text", order: 3),
            Field("from_email", "From Email", "text", mandatory: true, order: 4),
            Field("from_name", "From Name", "text", order: 5),
            Field("priority", "Priority", "number", order: 6),
            Field("smtp_host", "SMTP Host", "text", order: 7),
            Field("smtp_port", "SMTP Port", "number", order: 8),
            Field("smtp_username", "SMTP Username", "text", order: 9),
            Field("smtp_password", "SMTP Password", "password", order: 10),
            Field("gmail_client_id", "Gmail Client ID", "text", order: 11),
            Field("gmail_client_secret", "Gmail Client Secret", "password", order: 12),
            Field("gmail_refresh_token", "Gmail Refresh Token", "password", order: 13),
            Field("smtp_ssl", "SMTP SSL", "bit", order: 14),
            Field("is_active", "Active", "bit", order: 15),
        });

        await AddFieldsAsync(context, "rf_files_upload_type", cancellationToken, new[]
        {
            Field("files_upload_type_code", "Code", "text", key: true, mandatory: true, order: 1),
            Field("files_upload_type_T24_code", "T24 Code", "text", order: 2),
            Field("files_upload_type_name", "Name", "text", order: 3),
            Field("is_active", "Active", "bit", order: 4),
        });

        await AddFieldsAsync(context, "rf_group_access_report_set", cancellationToken, new[]
        {
            Field("id", "ID", "text", key: true, mandatory: true, order: 1),
            Field("group_id", "Group", "list", order: 2, reff: "select group_id as value, group_name as text from um_group_app where is_active=1 and is_deleted=0"),
            Field("report_set_id", "Report Set", "list", order: 3, reff: "select id as value, name as text from tb_report_set where is_active=1"),
            Field("is_active", "Active", "bit", order: 4),
        });

        await AddFieldsAsync(context, "rf_message", cancellationToken, new[]
        {
            Field("message_code", "Code", "text", key: true, mandatory: true, order: 1),
            Field("message_name", "Message", "text", order: 2),
            Field("is_active", "Active", "bit", order: 3),
            Field("is_default", "Default", "bit", order: 4),
        });

        await AddFieldsAsync(context, "tb_parameter_system", cancellationToken, new[]
        {
            Field("table_name", "Table Name", "text", key: true, mandatory: true, order: 1),
            Field("description", "Description", "text", order: 2),
            Field("param_maker_url", "Maker URL", "text", order: 3),
            Field("param_approval_url", "Approval URL", "text", order: 4),
            Field("is_param_maker", "Maker", "bit", order: 5),
            Field("is_param_approval", "Approval", "bit", order: 6),
            Field("is_param_upload", "Upload", "bit", order: 7),
            Field("icon", "Icon", "text", order: 8),
        });

        await AddFieldsAsync(context, "tb_parameter_system_detail", cancellationToken, new[]
        {
            Field("table_name", "Table Name", "text", key: true, mandatory: true, order: 1),
            Field("field_name", "Field Name", "text", key: true, mandatory: true, order: 2),
            Field("order_seq", "Order", "number", key: true, mandatory: true, order: 3),
            Field("field_desc", "Description", "text", order: 4),
            Field("field_key", "Key", "bit", order: 5),
            Field("field_type", "Type", "text", order: 6),
            Field("field_reff", "Reference", "text", order: 7),
            Field("is_mandatory", "Mandatory", "bit", order: 8),
            Field("is_group_by", "Group By", "bit", order: 9),
            Field("is_orderable", "Orderable", "bit", order: 10),
            Field("add_class", "CSS Class", "text", order: 11),
        });

        await AddFieldsAsync(context, "tb_report_set", cancellationToken, new[]
        {
            Field("id", "ID", "text", key: true, mandatory: true, order: 1),
            Field("name", "Name", "text", order: 2),
            Field("query_list", "Query List", "text", order: 3),
            Field("filter_param", "Filter Param", "text", order: 4),
            Field("detail_url", "Detail URL", "text", order: 5),
            Field("detail_param", "Detail Param", "text", order: 6),
            Field("query_list_param", "Query List Param", "text", order: 7),
            Field("status_approval", "Status Approval", "number", order: 8),
            Field("is_active", "Active", "bit", order: 9),
        });
    }

    private static async Task AddFieldsAsync(
        __PROJECT__DbContext context,
        string tableName,
        CancellationToken cancellationToken,
        IEnumerable<tb_parameter_system_detail> fields)
    {
        if (await context.tb_parameter_system_detail.AnyAsync(e => e.table_name == tableName, cancellationToken))
        {
            return;
        }

        foreach (var field in fields)
        {
            field.table_name = tableName;
            await context.tb_parameter_system_detail.AddAsync(field, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static tb_parameter_system_detail Field(
        string name,
        string description,
        string type,
        bool key = false,
        bool mandatory = false,
        int order = 0,
        string? reff = null) => new()
    {
        field_name = name,
        field_desc = description,
        field_type = type,
        field_key = key,
        is_mandatory = mandatory,
        order_seq = order,
        field_reff = reff,
        is_orderable = true,
    };

    private static async Task SeedReferenceDataAsync(__PROJECT__DbContext context, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        if (!await context.rf_default_value.AnyAsync(cancellationToken))
        {
            var defaults = new (string Code, string Name, string Value)[]
            {
                ("V030", "Password expiry days", "90"),
                ("V031", "Password warning days before expiry", "10"),
                ("V001", "Default age for rate in input", "35"),
                ("V005", "Default Plafon Max Capital", "0"),
                ("V006", "Default percent allocate gaji", "0"),
                ("V007", "Default percent provisi", "0"),
                ("V008", "Default percent premi asuransi", "0"),
                ("V018", "Default usia minimal berdasarkan rate asuransi", "18"),
                ("V021", "Default usia maksimal berdasarkan rate asuransi", "65"),
                ("V022", "Default waktu valid pengecekan SLIK dalam hari", "30"),
            };

            foreach (var item in defaults)
            {
                await context.rf_default_value.AddAsync(new rf_default_value
                {
                    def_code = item.Code,
                    def_name = item.Name,
                    def_value = item.Value,
                    statusApproval = Const.StatusApproval.APPROVED,
                    isActive = true,
                    isDeleted = false,
                    createdBy = "system",
                    createdDate = now,
                }, cancellationToken);
            }
        }

        if (!await context.rf_message.AnyAsync(cancellationToken))
        {
            var messages = new (string Code, string Name)[]
            {
                ("00", "Data berhasil di proses."),
                ("02", "Akun Anda telah di blokir."),
                ("03", "Salah password / sisa percobaan."),
                ("04", "First login, silakan ubah password."),
                ("11", "User Id sudah digunakan."),
                ("15", "Menu Unique sudah digunakan."),
                ("16", "Sesi Anda sudah habis."),
                ("19", "Data berhasil ditambahkan."),
                ("22", "Data berhasil diubah dan menunggu persetujuan."),
                ("25", "Data telah disetujui."),
            };

            foreach (var item in messages)
            {
                await context.rf_message.AddAsync(new rf_message
                {
                    message_code = item.Code,
                    message_name = item.Name,
                    statusApproval = Const.StatusApproval.APPROVED,
                    isActive = true,
                    isDeleted = false,
                    isDefault = item.Code == "00",
                    createdBy = "system",
                    createdDate = now,
                }, cancellationToken);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedScreensAsync(__PROJECT__DbContext context, CancellationToken cancellationToken)
    {
        if (!await context.tb_screen_menu_child.AnyAsync(e => e.screen_menu_child_id == "SMC-DEFAULT-VALUE", cancellationToken))
        {
            await context.tb_screen_menu_child.AddAsync(new tb_screen_menu_child
            {
                screen_menu_child_id = "SMC-DEFAULT-VALUE",
                description = "Default Value Modal",
                type = "modal",
                modal_field = "[DefCode|Code:v];[DefName|Name:v];[DefValue|Value:v];[IsActive|Active:cb]",
                modal_detail_field_query = "SELECT def_code AS DefCode, def_name AS DefName, def_value AS DefValue, is_active AS IsActive FROM rf_default_value WHERE def_code = '{0}'",
                modal_detail_field_query_param = "[DefCode]",
                modal_save_process_query = "MERGE rf_default_value AS target USING (SELECT '{0}' AS def_code) AS source ON target.def_code = source.def_code WHEN MATCHED THEN UPDATE SET def_name = '{1}', def_value = '{2}', is_active = {3} WHEN NOT MATCHED THEN INSERT (def_code, def_name, def_value, is_active) VALUES ('{0}', '{1}', '{2}', {3});",
                modal_save_process_query_param = "[DefCode];[DefName];[DefValue];[IsActive]",
                isActive = true,
                isDeleted = false,
                createdBy = "system",
                createdDate = DateTime.Now,
            }, cancellationToken);
        }

        if (!await context.tb_screen_menu_child.AnyAsync(e => e.screen_menu_child_id == "SMC-URL-DEMO", cancellationToken))
        {
            await context.tb_screen_menu_child.AddAsync(new tb_screen_menu_child
            {
                screen_menu_child_id = "SMC-URL-DEMO",
                description = "Default Value URL",
                type = "url",
                url_action = "https://example.com/?code={0}",
                url_action_param = "[def_code]",
                isActive = true,
                isDeleted = false,
                createdBy = "system",
                createdDate = DateTime.Now,
            }, cancellationToken);
        }

        if (!await context.tb_screen_menu.AnyAsync(e => e.id == "SM-URL-DEMO", cancellationToken))
        {
            await context.tb_screen_menu.AddAsync(new tb_screen_menu
            {
                id = "SM-URL-DEMO",
                description = "URL Demo",
                has_button_load = true,
                has_button_process = false,
                has_button_generate_txt = false,
                has_button_export_excel = false,
                has_button_add = false,
                has_button_delete = false,
                has_action_row = true,
                has_button_import_excel = false,
                field_filter = "[DefCode|Code:v];[DefName|Name:v]",
                query_load = "SELECT def_code, def_name FROM rf_default_value",
                screen_menu_child_id = "SMC-URL-DEMO",
                statusApproval = Const.StatusApproval.APPROVED,
                isActive = true,
                isDeleted = false,
                createdBy = "system",
                createdDate = DateTime.Now,
            }, cancellationToken);
        }

        if (!await context.tb_screen_menu.AnyAsync(e => e.id == "SM-LOG", cancellationToken))
        {
            await context.tb_screen_menu.AddAsync(new tb_screen_menu
            {
                id = "SM-LOG",
                description = "Log",
                has_button_load = true,
                has_button_export_excel = true,
                has_button_process = false,
                has_button_generate_txt = false,
                has_button_add = false,
                has_button_delete = false,
                has_action_row = false,
                has_button_import_excel = false,
                field_filter = "[LogType|Log Type:l:SELECT DISTINCT log_type as value, log_type as text FROM [dbo].[tb_log]];[DateFrom|From Date:d];[DateTo|To Date:d]",
                query_load = "SELECT log_type, log_msg, created_date, created_by FROM [dbo].[tb_log] WHERE log_type NOT LIKE '%Check User Login%' AND log_type NOT LIKE '%CheckSession%' AND log_type NOT LIKE '%Check Group Login%'",
                statusApproval = Const.StatusApproval.APPROVED,
                isActive = true,
                isDeleted = false,
                createdBy = "system",
                createdDate = DateTime.Now,
            }, cancellationToken);
        }

        if (!await context.tb_screen_menu_import_excel_process.AnyAsync(e => e.screen_menu_import_excel_process_id == "IMP-DEFAULT-VALUE", cancellationToken))
        {
            await context.tb_screen_menu_import_excel_process.AddAsync(new tb_screen_menu_import_excel_process
            {
                screen_menu_import_excel_process_id = "IMP-DEFAULT-VALUE",
                description = "Import Default Value",
                table_name = "rf_default_value",
                field_input = "Code=def_code,Name=def_name,Value=def_value,Active=is_active",
                with_approval = false,
                isActive = true,
                isDeleted = false,
                createdBy = "system",
                createdDate = DateTime.Now,
            }, cancellationToken);
        }

        if (!await context.tb_screen_menu.AnyAsync(e => e.id == "SM-DEFAULT-VALUE", cancellationToken))
        {
            await context.tb_screen_menu.AddAsync(new tb_screen_menu
            {
                id = "SM-DEFAULT-VALUE",
                description = "Default Value",
                has_button_load = true,
                has_button_process = false,
                has_button_generate_txt = true,
                has_button_export_excel = true,
                has_button_add = true,
                has_button_delete = true,
                has_action_row = true,
                has_button_import_excel = true,
                field_filter = "[DefCode|Code:v];[DefName|Name:v];[IsActive|Active:cb]",
                query_load = "SELECT def_code, def_name, def_value, is_active FROM rf_default_value WHERE COALESCE({0}, '') = '' OR def_code LIKE '%' + COALESCE({0}, '') + '%' OR def_name LIKE '%' + COALESCE({0}, '') + '%'",
                query_load_param = "[searchTerm]",
                query_delete = "DELETE FROM rf_default_value WHERE def_code = '{0}'",
                query_delete_param = "[def_code]",
                screen_menu_child_id = "SMC-DEFAULT-VALUE",
                screen_menu_import_excel_process_id = "IMP-DEFAULT-VALUE",
                statusApproval = Const.StatusApproval.APPROVED,
                isActive = true,
                isDeleted = false,
                createdBy = "system",
                createdDate = DateTime.Now,
            }, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedReportAsync(__PROJECT__DbContext context, CancellationToken cancellationToken)
    {
        if (await context.tb_report_set.AnyAsync(e => e.id == "LOG", cancellationToken))
        {
            return;
        }

        await context.tb_report_set.AddAsync(new tb_report_set
        {
            id = "LOG",
            name = "Log",
            query_list = "SELECT log_type, log_msg, created_date, created_by FROM [dbo].[tb_log] WHERE log_type NOT LIKE '%Check User Login%' AND log_type NOT LIKE '%CheckSession%' AND log_type NOT LIKE '%Check Group Login%'",
            statusApproval = Const.StatusApproval.APPROVED,
            isActive = true,
            isDeleted = false,
            createdBy = "system",
            createdDate = DateTime.Now,
        }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedAccessAsync(__PROJECT__DbContext context, CancellationToken cancellationToken)
    {
        if (await context.rf_group_access_report_set.AnyAsync(e => e.id == "GA001", cancellationToken))
        {
            return;
        }

        await context.rf_group_access_report_set.AddAsync(new rf_group_access_report_set
        {
            id = "GA001",
            group_id = Const.DEFAULT_GROUP_ID_ADMINISTRATOR_CODE,
            report_set_id = "LOG",
            statusApproval = Const.StatusApproval.APPROVED,
            isActive = true,
            isDeleted = false,
            createdBy = "system",
            createdDate = DateTime.Now,
        }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
