type PageProps = {
  params: Promise<{ slug: string }>;
};

export async function generateMetadata({ params }: PageProps) {
  const { slug } = await params;
  const label = decodeURIComponent(slug).replace(/-/g, " ");
  return { title: label.charAt(0).toUpperCase() + label.slice(1) };
}

export default async function CategoryPage({ params }: PageProps) {
  const { slug } = await params;
  const label = decodeURIComponent(slug).replace(/-/g, " ");

  return (
    <div className="mx-auto max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
      <h1 className="text-2xl font-semibold capitalize text-neutral-900">{label}</h1>
      <p className="mt-2 text-neutral-600">Category listings will appear here once wired to the catalogue.</p>
    </div>
  );
}
