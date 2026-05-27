import { Button } from "@/components/ui/button";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";
import { defaultGeneratingRequest } from "@/config/defaults";
import { useRecipesStore } from "@/features/recipes/hooks/use-recipes-store";
import { PlayIcon } from "@/lib/icons";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { z } from "zod";

const schema = z.object({
  recipeId: z.number().min(1, "Chọn một recipe"),
  name: z.string().min(1, "Nhập tên lần xuất"),
  outputType: z.enum(["Potx", "Pptx", "Ppsx"]),
  saveFolder: z.string().min(1, "Nhập thư mục lưu"),
  downloadAssetsPath: z.string().optional(),
  editAssetsPath: z.string().optional(),
  allowLocalImagePaths: z.boolean(),
});

type FormValues = z.infer<typeof schema>;

export function StudioConfigTab() {
  const { t } = useTranslation();
  const { recipes } = useRecipesStore();

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      ...defaultGeneratingRequest,
      recipeId: defaultGeneratingRequest.recipeId,
    },
  });

  const onSubmit = (values: FormValues) => {
    console.log("Generating request:", values);
    toast.success(`Đã xếp lịch: "${values.name}"`);
  };

  return (
    <div className="p-6 max-w-2xl w-full">
      <div className="mb-6">
        <h2
          className="text-xl font-semibold text-[color:var(--foreground)]"
          style={{ fontFamily: "var(--font-heading)" }}
        >
          {t("studio.config.title")}
        </h2>
      </div>

      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-5">
          {/* Recipe selector */}
          <FormField
            control={form.control}
            name="recipeId"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t("studio.config.recipe")}</FormLabel>
                <Select
                  value={String(field.value)}
                  onValueChange={(v) => field.onChange(Number(v))}
                >
                  <FormControl>
                    <SelectTrigger className="rounded-xl">
                      <SelectValue placeholder={t("studio.config.selectRecipe")} />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    {recipes.map((r) => (
                      <SelectItem key={r.id} value={String(r.id)}>
                        {r.displayName ?? `Recipe #${r.id}`}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <FormMessage />
              </FormItem>
            )}
          />

          {/* Name */}
          <FormField
            control={form.control}
            name="name"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t("studio.config.name")}</FormLabel>
                <FormControl>
                  <Input {...field} className="rounded-xl" />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          {/* Output type */}
          <FormField
            control={form.control}
            name="outputType"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t("studio.config.outputType")}</FormLabel>
                <Select value={field.value} onValueChange={field.onChange}>
                  <FormControl>
                    <SelectTrigger className="rounded-xl">
                      <SelectValue />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    <SelectItem value="Pptx">.pptx</SelectItem>
                    <SelectItem value="Ppsx">.ppsx</SelectItem>
                    <SelectItem value="Potx">.potx</SelectItem>
                  </SelectContent>
                </Select>
                <FormMessage />
              </FormItem>
            )}
          />

          {/* Save folder */}
          <FormField
            control={form.control}
            name="saveFolder"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t("studio.config.saveFolder")}</FormLabel>
                <FormControl>
                  <Input {...field} className="rounded-xl font-mono text-sm" />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          {/* Download assets path */}
          <FormField
            control={form.control}
            name="downloadAssetsPath"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t("studio.config.downloadAssetsPath")}</FormLabel>
                <FormControl>
                  <Input {...field} className="rounded-xl font-mono text-sm" />
                </FormControl>
              </FormItem>
            )}
          />

          {/* Edit assets path */}
          <FormField
            control={form.control}
            name="editAssetsPath"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t("studio.config.editAssetsPath")}</FormLabel>
                <FormControl>
                  <Input {...field} className="rounded-xl font-mono text-sm" />
                </FormControl>
              </FormItem>
            )}
          />

          {/* Allow local image paths */}
          <FormField
            control={form.control}
            name="allowLocalImagePaths"
            render={({ field }) => (
              <FormItem className="flex items-center justify-between">
                <FormLabel className="mb-0">{t("studio.config.allowLocalImagePaths")}</FormLabel>
                <FormControl>
                  <Switch checked={field.value} onCheckedChange={field.onChange} />
                </FormControl>
              </FormItem>
            )}
          />

          <Button type="submit" className="rounded-full gap-2 mt-2">
            <PlayIcon size={16} />
            {t("studio.config.submit")}
          </Button>
        </form>
      </Form>
    </div>
  );
}
